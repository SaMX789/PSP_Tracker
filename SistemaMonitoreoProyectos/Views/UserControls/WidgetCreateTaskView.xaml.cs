using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class WidgetCreateTaskView : UserControl
    {
        private readonly ActividadRepository _actividadRepository = new ActividadRepository();
        private readonly FaseRepository _faseRepository = new FaseRepository();
        private readonly PlanFaseRepository _planFaseRepository = new PlanFaseRepository();

        private readonly ObservableCollection<FasePlanItemDTO> _fasesPlan = new ObservableCollection<FasePlanItemDTO>();
        private bool _isSincronizandoSuma = false;
        private bool _editandoDesdeSubfases = false;

        public WidgetCreateTaskView()
        {
            InitializeComponent();
            Loaded += WidgetCreateTaskView_Loaded;
        }

        private void WidgetCreateTaskView_Loaded(object sender, RoutedEventArgs e)
        {
            CargarFasesVacias();
        }

        private void CargarFasesVacias()
        {
            var todasLasFases = _faseRepository.ObtenerTodas();
            _fasesPlan.Clear();

            var nombresEspanol = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Planning", "Planificar tu proyecto" },
                { "Design", "Diseño de solución" },
                { "Design Review", "Revisión de diseño" },
                { "Code", "Programación / Código" },
                { "Code Review", "Revisión de código" },
                { "Compile", "Compilación" },
                { "Test", "Pruebas del sistema" },
                { "Postmortem", "Análisis final (Postmortem)" }
            };

            foreach (var fase in todasLasFases)
            {
                string nombreDisplay = nombresEspanol.ContainsKey(fase.Nombre) ? nombresEspanol[fase.Nombre] : fase.Nombre;

                var item = new FasePlanItemDTO
                {
                    FaseId = fase.Id,
                    NombreFaseOriginal = fase.Nombre,
                    NombreFaseDisplay = nombreDisplay,
                    TiempoEstimadoHoras = 0,
                    DefectosInyectados = 0,
                    DefectosRemovidos = 0
                };

                item.OnTiempoCambiado += RecalcularSumaTotalHoras;
                _fasesPlan.Add(item);
            }

            if (ListaFasesPlan != null)
            {
                ListaFasesPlan.ItemsSource = _fasesPlan;
            }
        }

        // LIMPIAR LAS FASES DE ABAJO CUANDO SE SELECCIONA O EDITA EL CAMPO DE ARRIBA
        private void TextoTiempoEstimado_GotFocus(object sender, RoutedEventArgs e)
        {
            LimpiarHorasSubfases();
        }

        private void TextoTiempoEstimado_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || _editandoDesdeSubfases) return;
            LimpiarHorasSubfases();
        }

        private void LimpiarHorasSubfases()
        {
            if (_isSincronizandoSuma) return;

            _isSincronizandoSuma = true;
            foreach (var fase in _fasesPlan)
            {
                fase.TiempoEstimadoHoras = 0;
            }
            _isSincronizandoSuma = false;
        }

        // SUMA AUTOMÁTICA HACIA ARRIBA CUANDO SE ESCRIBE ABAJO
        private void RecalcularSumaTotalHoras()
        {
            if (_isSincronizandoSuma) return;

            double sumaHorasFases = _fasesPlan.Sum(f => f.TiempoEstimadoHoras);
            bool tieneFases = _fasesPlan.Any(f => f.TiempoEstimadoHoras > 0 || !string.IsNullOrWhiteSpace(f.TiempoEstimadoHorasTexto));

            if (tieneFases)
            {
                _editandoDesdeSubfases = true;
                _isSincronizandoSuma = true;
                TextoTiempoEstimado.Text = sumaHorasFases > 0 ? sumaHorasFases.ToString("0.##", CultureInfo.InvariantCulture) : "";
                _isSincronizandoSuma = false;
                _editandoDesdeSubfases = false;
            }
        }

        private void BotonSiguiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // VALIDACIÓN: Título, Descripción y Responsable son obligatorios
                if (string.IsNullOrWhiteSpace(TextoTituloActividad.Text) ||
                    string.IsNullOrWhiteSpace(TextoDescripcionActividad.Text) ||
                    string.IsNullOrWhiteSpace(TextoResponsable.Text))
                {
                    MessageBox.Show("Por favor, completa los campos principales (Título, Descripción y Responsable).", "Campos Requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // REGLA: Si hay fases vacías o con 0 tiempo, se les asigna automáticamente 1 hora (1.0), 0 inyectados y 0 removidos
                foreach (var fase in _fasesPlan)
                {
                    if (fase.TiempoEstimadoHoras <= 0)
                    {
                        fase.TiempoEstimadoHoras = 1.0;
                        fase.DefectosInyectados = 0;
                        fase.DefectosRemovidos = 0;
                    }
                }

                // Recalcular la suma final acumulada tras rellenar las fases vacías
                double sumaHorasFases = _fasesPlan.Sum(f => f.TiempoEstimadoHoras);
                TextoTiempoEstimado.Text = sumaHorasFases.ToString("0.##", CultureInfo.InvariantCulture);
                int minutosFinales = (int)Math.Round(sumaHorasFases * 60);

                // 1. Guardar Actividad
                var nuevaActividad = new Actividad
                {
                    Proyecto = TextoTituloActividad.Text.Trim(),
                    Descripcion = TextoDescripcionActividad.Text.Trim(),
                    Responsable = TextoResponsable.Text.Trim(),
                    TiempoEstimadoMinutos = minutosFinales,
                    FechaInicio = DateTime.Now,
                    Estado = 0
                };

                int nuevoId = _actividadRepository.Agregar(nuevaActividad);

                // 2. Guardar Plan por Fases
                var listaPlanFases = _fasesPlan.Select(f => new PlanFase
                {
                    ActividadId = nuevoId,
                    FaseId = f.FaseId,
                    TiempoEstimadoMinutos = (int)Math.Round(f.TiempoEstimadoHoras * 60),
                    DefectosEstimadosInyectados = f.DefectosInyectados,
                    DefectosEstimadosRemovidos = f.DefectosRemovidos
                }).ToList();

                _planFaseRepository.GuardarOActualizarLista(listaPlanFases);

                // 3. Inicializar Estado de la Sesión
                var estadoSesionRepository = new EstadoSesionRepository();
                estadoSesionRepository.GuardarOSustituirSesion(new EstadoSesion
                {
                    ActividadId = nuevoId,
                    FaseActualId = _fasesPlan.FirstOrDefault()?.FaseId ?? 1,
                    EstadoCronometro = 0,
                    MinutosAcumulados = 0
                });

                // 4. Iniciar Temporizador en el Widget
                if (Window.GetWindow(this) is WidgetWindow widgetWindow)
                {
                    widgetWindow.CargarTimeActividadCreada();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la actividad:\n\n{ex.Message}", "Error de SQLite", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas();
            }
        }

        private void BotonOpcionesWidget_Click(object sender, RoutedEventArgs e)
        {
            if (BotonOpcionesWidget.ContextMenu != null)
            {
                BotonOpcionesWidget.ContextMenu.IsOpen = true;
            }
        }

        private void MenuItemSalirWidget_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.Close();
            }
        }

        // --- MÉTODOS DE RESTRICCIÓN DE ENTRADA TECLA A TECLA ---

        private void SoloDecimales_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[0-9.,]$"))
            {
                e.Handled = true;
                return;
            }

            if ((e.Text == "." || e.Text == ",") && sender is TextBox tb)
            {
                if (tb.Text.Contains(".") || tb.Text.Contains(","))
                {
                    e.Handled = true;
                }
            }
        }

        private void SoloEnteros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]$");
        }

        private void SinEspacios_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }

    public class FasePlanItemDTO : INotifyPropertyChanged
    {
        public int FaseId { get; set; }
        public string NombreFaseOriginal { get; set; } = string.Empty;
        public string NombreFaseDisplay { get; set; } = string.Empty;

        public Action? OnTiempoCambiado;

        // Propiedades dinámicas de estado
        public bool PuedeEditarInyectados => TiempoEstimadoHoras > 0;
        public bool PuedeEditarRemovidos => PuedeEditarInyectados && DefectosInyectados > 0;

        private double _tiempoEstimadoHoras = 0;
        public double TiempoEstimadoHoras
        {
            get => _tiempoEstimadoHoras;
            set
            {
                if (_tiempoEstimadoHoras != value)
                {
                    _tiempoEstimadoHoras = value;

                    string newText = value == 0 ? "" : value.ToString(CultureInfo.InvariantCulture);
                    if (_tiempoEstimadoHorasTexto != newText)
                    {
                        _tiempoEstimadoHorasTexto = newText;
                        OnPropertyChanged(nameof(TiempoEstimadoHorasTexto));
                    }

                    // Si el tiempo es <= 0, resetea inyectados y removidos a 0
                    if (_tiempoEstimadoHoras <= 0)
                    {
                        DefectosInyectados = 0;
                        DefectosRemovidos = 0;
                    }

                    OnPropertyChanged(nameof(PuedeEditarInyectados));
                    OnPropertyChanged(nameof(PuedeEditarRemovidos));
                    OnTiempoCambiado?.Invoke();
                }
            }
        }

        private string _tiempoEstimadoHorasTexto = "";
        public string TiempoEstimadoHorasTexto
        {
            get => _tiempoEstimadoHorasTexto;
            set
            {
                if (_tiempoEstimadoHorasTexto != value)
                {
                    _tiempoEstimadoHorasTexto = value;
                    OnPropertyChanged();

                    string val = value.Replace(',', '.');
                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                    {
                        if (_tiempoEstimadoHoras != parsed)
                        {
                            _tiempoEstimadoHoras = parsed;
                            if (_tiempoEstimadoHoras <= 0)
                            {
                                DefectosInyectados = 0;
                                DefectosRemovidos = 0;
                            }
                            OnPropertyChanged(nameof(PuedeEditarInyectados));
                            OnPropertyChanged(nameof(PuedeEditarRemovidos));
                            OnTiempoCambiado?.Invoke();
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(val))
                    {
                        if (_tiempoEstimadoHoras != 0)
                        {
                            _tiempoEstimadoHoras = 0;
                            DefectosInyectados = 0;
                            DefectosRemovidos = 0;
                            OnPropertyChanged(nameof(PuedeEditarInyectados));
                            OnPropertyChanged(nameof(PuedeEditarRemovidos));
                            OnTiempoCambiado?.Invoke();
                        }
                    }
                }
            }
        }

        private int _defectosInyectados = 0;
        public int DefectosInyectados
        {
            get => _defectosInyectados;
            set
            {
                if (_defectosInyectados != value)
                {
                    _defectosInyectados = value;
                    string newText = value == 0 ? "" : value.ToString();
                    if (_defectosInyectadosTexto != newText)
                    {
                        _defectosInyectadosTexto = newText;
                        OnPropertyChanged(nameof(DefectosInyectadosTexto));
                    }
                    OnPropertyChanged(nameof(PuedeEditarRemovidos));
                    AjustarDefectosRemovidos();
                }
            }
        }

        private string _defectosInyectadosTexto = "";
        public string DefectosInyectadosTexto
        {
            get => _defectosInyectadosTexto;
            set
            {
                if (_defectosInyectadosTexto != value)
                {
                    _defectosInyectadosTexto = value;
                    OnPropertyChanged();

                    if (int.TryParse(value, out int parsed) && parsed > 0)
                    {
                        _defectosInyectados = parsed;
                    }
                    else
                    {
                        _defectosInyectados = 0;
                    }

                    OnPropertyChanged(nameof(PuedeEditarRemovidos));
                    AjustarDefectosRemovidos();
                }
            }
        }

        private int _defectosRemovidos = 0;
        public int DefectosRemovidos
        {
            get => _defectosRemovidos;
            set
            {
                if (_defectosRemovidos != value)
                {
                    _defectosRemovidos = value;
                    string newText = value == 0 ? "" : value.ToString();
                    if (_defectosRemovidosTexto != newText)
                    {
                        _defectosRemovidosTexto = newText;
                        OnPropertyChanged(nameof(DefectosRemovidosTexto));
                    }
                }
            }
        }

        private string _defectosRemovidosTexto = "";
        public string DefectosRemovidosTexto
        {
            get => _defectosRemovidosTexto;
            set
            {
                if (_defectosRemovidosTexto != value)
                {
                    int valAAplicar = 0;
                    if (int.TryParse(value, out int parsed))
                    {
                        valAAplicar = parsed;
                        if (valAAplicar > _defectosInyectados)
                        {
                            valAAplicar = _defectosInyectados;
                        }
                    }

                    _defectosRemovidos = valAAplicar;
                    _defectosRemovidosTexto = valAAplicar == 0 && string.IsNullOrWhiteSpace(value)
                        ? ""
                        : valAAplicar.ToString();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DefectosRemovidosTexto));
                }
            }
        }

        private void AjustarDefectosRemovidos()
        {
            if (_defectosInyectados == 0)
            {
                DefectosRemovidos = 0;
                _defectosRemovidosTexto = "";
                OnPropertyChanged(nameof(DefectosRemovidosTexto));
            }
            else if (_defectosRemovidos > _defectosInyectados)
            {
                DefectosRemovidos = _defectosInyectados;
                _defectosRemovidosTexto = _defectosInyectados.ToString();
                OnPropertyChanged(nameof(DefectosRemovidosTexto));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}