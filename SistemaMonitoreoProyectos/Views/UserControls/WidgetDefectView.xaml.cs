using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class WidgetDefectView : UserControl
    {
        private readonly IEstadoSesionRepository _sesionRepo;
        private readonly IFaseRepository _faseRepo;
        private readonly IRegistroDefectoRepository _defectoRepo;
        private readonly ITipoDefectoRepository _tipoDefectoRepo;

        private readonly DispatcherTimer _timerDefectoUI = new DispatcherTimer();
        private DateTime _fechaInicioDefecto;
        private int _segundosHistoricosPrevios = 0;

        private long? _defectoIdActual;
        private long? _defectoPadreId;
        private bool _yaEstaResuelto = false;

        public WidgetDefectView(long? defectoIdParaCargar = null, long? defectoPadreId = null)
        {
            InitializeComponent();
            _defectoIdActual = defectoIdParaCargar;
            _defectoPadreId = defectoPadreId;

            _sesionRepo = new EstadoSesionRepository();
            _faseRepo = new FaseRepository();
            _defectoRepo = new RegistroDefectoRepository();
            _tipoDefectoRepo = new TipoDefectoRepository();

            _timerDefectoUI.Interval = TimeSpan.FromSeconds(1);
            _timerDefectoUI.Tick += (s, e) => ActualizarRelojDefectoPantalla();

            Loaded += WidgetDefectView_Loaded;
            Unloaded += (s, e) => _timerDefectoUI.Stop();
        }

        private void WidgetDefectView_Loaded(object sender, RoutedEventArgs e)
        {
            CargarCatalogosFases();

            // 1. Cargar datos desde SQLite primero para verificar si ya está resuelto
            if (_defectoIdActual.HasValue)
            {
                CargarDatosDefectoExistente(_defectoIdActual.Value);
            }

            // 2. Si YA ESTÁ RESUELTO, NO se inicia el cronómetro (tiempo congelado)
            if (_yaEstaResuelto)
            {
                _timerDefectoUI.Stop();
            }
            else
            {
                _fechaInicioDefecto = DateTime.Now;
                _timerDefectoUI.Start();
            }

            CargarSubDefectosAnidados();
            ActualizarRelojDefectoPantalla();
        }

        private void CargarCatalogosFases()
        {
            var fases = _faseRepo.ObtenerTodas();
            var sesion = _sesionRepo.ObtenerSesion();

            DesplegableFaseOrigen.ItemsSource = fases;
            DesplegableFaseDeteccion.ItemsSource = fases;

            int faseActiva = sesion.FaseActualId ?? 1;
            DesplegableFaseDeteccion.SelectedValue = faseActiva;
            DesplegableFaseOrigen.SelectedValue = faseActiva;

            var tipos = _tipoDefectoRepo.ObtenerTodos();
            DesplegableTipoDefecto.ItemsSource = tipos;
            if (tipos.Any())
            {
                DesplegableTipoDefecto.SelectedValue = 80; // Default a 80 (Función/Lógica)
            }
        }

        private void CargarDatosDefectoExistente(long defectoId)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue) return;

            var lista = _defectoRepo.ObtenerPorActividad(sesion.ActividadId.Value);
            var defecto = lista.FirstOrDefault(d => d.Id == defectoId);

            if (defecto != null)
            {
                TextoDescripcionDefecto.Text = defecto.DescripcionError;
                DesplegableFaseOrigen.SelectedValue = defecto.FaseOrigenId;
                DesplegableFaseDeteccion.SelectedValue = defecto.FaseDeteccionId;

                if (defecto.TipoDefectoId.HasValue)
                {
                    DesplegableTipoDefecto.SelectedValue = defecto.TipoDefectoId.Value;
                }

                _defectoPadreId = defecto.DefectoPadreId;

                _segundosHistoricosPrevios = (int)defecto.TiempoCorreccionMinutos;

                if (defecto.EsResuelto == 1)
                {
                    _yaEstaResuelto = true;
                    BotonMarcarListo.IsEnabled = false;
                    BotonMarcarListo.ToolTip = "Este defecto ya fue resuelto y su estado es permanente.";
                }
            }
        }

        private void CargarSubDefectosAnidados()
        {
            if (ContenedorSubDefectos == null) return;
            ContenedorSubDefectos.Children.Clear();

            if (!_defectoIdActual.HasValue) return;

            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue) return;

            var todos = _defectoRepo.ObtenerPorActividad(sesion.ActividadId.Value);
            var subDefectos = todos.Where(d => d.DefectoPadreId == _defectoIdActual.Value).ToList();

            for (int i = 0; i < subDefectos.Count; i++)
            {
                var sub = subDefectos[i];
                int numeroSub = i + 1;

                string colorFondo = sub.EsResuelto == 1 ? "#10B981" : "#FF3B30";
                string colorHover = sub.EsResuelto == 1 ? "#059669" : "#E02D22";

                TimeSpan tiempoSub = TimeSpan.FromSeconds(sub.TiempoCorreccionMinutos);
                string tiempoFormateado = tiempoSub.ToString(@"hh\:mm\:ss");

                var btnBolita = CrearBotonBolita(numeroSub.ToString(), colorFondo, colorHover,
                    $"Sub-defecto #{numeroSub}: {sub.DescripcionError} ({tiempoFormateado})");

                btnBolita.Click += (s, e) =>
                {
                    GuardarOActualizarDefecto(marcarComoResuelto: false);
                    if (Window.GetWindow(this) is WidgetWindow widget)
                    {
                        widget.CargarVistaDefecto(defectoIdParaCargar: sub.Id);
                    }
                };

                ContenedorSubDefectos.Children.Add(btnBolita);
            }
        }

        private Button CrearBotonBolita(string texto, string colorHex, string hoverHex, string tooltip)
        {
            var btn = new Button
            {
                Content = texto,
                Width = 28,
                Height = 28,
                Margin = new Thickness(4, 2, 4, 2),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
            borderFactory.SetValue(Border.BackgroundProperty, (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(colorHex)!);

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(hoverHex)!, "border"));

            template.VisualTree = borderFactory;
            template.Triggers.Add(trigger);
            btn.Template = template;

            return btn;
        }

        private void ActualizarRelojDefectoPantalla()
        {
            // Si ya está resuelto, el tiempo transcurrido en la sesión actual es 0
            int segundosSesionActual = _yaEstaResuelto ? 0 : (int)(DateTime.Now - _fechaInicioDefecto).TotalSeconds;
            int segundosTotales = _segundosHistoricosPrevios + segundosSesionActual;

            TimeSpan transcurrido = TimeSpan.FromSeconds(segundosTotales);
            if (TextoRelojDefecto != null)
            {
                TextoRelojDefecto.Text = transcurrido.ToString(@"hh\:mm\:ss");
            }
        }

        private void BotonMarcarListo_Click(object sender, RoutedEventArgs e)
        {
            if (_defectoIdActual.HasValue && TieneSubDefectosPendientes(_defectoIdActual.Value))
            {
                MessageBox.Show(
                    "No puedes marcar como listo este defecto porque aún contiene sub-defectos pendientes (en rojo). Resuelve primero todos los sub-defectos derivados.",
                    "Sub-defectos pendientes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            GuardarOActualizarDefecto(marcarComoResuelto: true);
            RegresarAlTemporizador();
        }

        private bool TieneSubDefectosPendientes(long defectoId)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue) return false;

            var todos = _defectoRepo.ObtenerPorActividad(sesion.ActividadId.Value);
            return VerificarPendientesRecursivo(defectoId, todos);
        }

        private bool VerificarPendientesRecursivo(long padreId, List<RegistroDefecto> todos)
        {
            var hijos = todos.Where(d => d.DefectoPadreId == padreId).ToList();
            foreach (var hijo in hijos)
            {
                if (hijo.EsResuelto == 0) return true;
                if (VerificarPendientesRecursivo(hijo.Id, todos)) return true;
            }
            return false;
        }

        private void BotonOpciones_Click(object sender, RoutedEventArgs e)
        {
            if (BotonOpciones.ContextMenu != null)
            {
                BotonOpciones.ContextMenu.IsOpen = true;
            }
        }

        private void MenuItemGuardarAtras_Click(object sender, RoutedEventArgs e)
        {
            GuardarOActualizarDefecto(marcarComoResuelto: false);
            RegresarAlTemporizador();
        }

        private void MenuItemEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_defectoIdActual.HasValue)
            {
                _defectoRepo.Eliminar(_defectoIdActual.Value);
            }
            RegresarAlTemporizador();
        }

        private void BotonAgregarSubDefecto_Click(object sender, RoutedEventArgs e)
        {
            long idPadre = GuardarOActualizarDefecto(marcarComoResuelto: false);

            if (Window.GetWindow(this) is WidgetWindow widget)
            {
                widget.CargarVistaDefecto(defectoIdParaCargar: null, defectoPadreId: idPadre);
            }
        }

        private void BotonCerrarDefecto_Click(object sender, RoutedEventArgs e)
        {
            GuardarOActualizarDefecto(marcarComoResuelto: false);
            RegresarAlTemporizador();
        }

        private void RegresarAlTemporizador()
        {
            if (Window.GetWindow(this) is WidgetWindow widget)
            {
                widget.CargarTimeActividadCreada();
            }
        }

        private long GuardarOActualizarDefecto(bool marcarComoResuelto)
        {
            _timerDefectoUI.Stop();
            var sesion = _sesionRepo.ObtenerSesion();

            // Si el defecto ya estaba resuelto, no se le suma ningún segundo nuevo
            int segundosSesionActual = _yaEstaResuelto ? 0 : (int)(DateTime.Now - _fechaInicioDefecto).TotalSeconds;
            long segundosTotalesAcc = _segundosHistoricosPrevios + segundosSesionActual;

            string descripcion = string.IsNullOrWhiteSpace(TextoDescripcionDefecto.Text)
                ? "Defecto sin descripción"
                : TextoDescripcionDefecto.Text.Trim();

            long faseOrigen = DesplegableFaseOrigen.SelectedValue != null ? Convert.ToInt64(DesplegableFaseOrigen.SelectedValue) : 1;
            long faseDeteccion = DesplegableFaseDeteccion.SelectedValue != null ? Convert.ToInt64(DesplegableFaseDeteccion.SelectedValue) : 1;

            int? tipoSeleccionado = DesplegableTipoDefecto.SelectedValue != null ? Convert.ToInt32(DesplegableTipoDefecto.SelectedValue) : (int?)null;
            
            int estadoFinalResuelto = (_yaEstaResuelto || marcarComoResuelto) ? 1 : 0;

            var registro = new RegistroDefecto
            {
                Id = _defectoIdActual ?? 0,
                ActividadId = sesion.ActividadId ?? 0,
                DefectoPadreId = _defectoPadreId,
                DescripcionError = descripcion,
                FaseOrigenId = faseOrigen,
                FaseDeteccionId = faseDeteccion,
                TipoDefectoId = tipoSeleccionado,
                TiempoCorreccionMinutos = segundosTotalesAcc, // Guarda exactamente el acumulado sin sumar extras si ya estaba listo
                FechaRegistro = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                EsResuelto = estadoFinalResuelto
            };

            if (_defectoIdActual.HasValue && _defectoIdActual.Value > 0)
            {
                _defectoRepo.Actualizar(registro);
                return _defectoIdActual.Value;
            }
            else
            {
                long nuevoId = _defectoRepo.Agregar(registro);
                _defectoIdActual = nuevoId;
                return nuevoId;
            }
        }
    }
}