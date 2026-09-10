using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class WidgetActiveTimerView : UserControl
    {
        private readonly IEstadoSesionRepository _sesionRepo;
        private readonly IRegistroEsfuerzoRepository _registroRepo;
        private readonly IFaseRepository _faseRepo;
        private readonly DispatcherTimer _timerRelojUI = new DispatcherTimer();

        private int _segundosHistoricosOtros = 0;
        private DateTime? _fechaInicioTramo;
        private bool _isCargandoFases = false;
        private long _ultimoRegistroEsfuerzoId = 0;
        private DateTime? _inicioInterrupcion;
        private readonly IInterrupcionRepository _interrupcionRepo = new InterrupcionRepository();
        private readonly IRegistroDefectoRepository _defectoRepo = new RegistroDefectoRepository();
        private List<FaseItemView> _listaFasesUI = new List<FaseItemView>();

        public WidgetActiveTimerView()
        {
            InitializeComponent();
            _sesionRepo = new EstadoSesionRepository();
            _registroRepo = new RegistroEsfuerzoRepository();
            _faseRepo = new FaseRepository();

            _timerRelojUI.Interval = TimeSpan.FromSeconds(1);
            _timerRelojUI.Tick += TimerRelojUI_Tick;

            Loaded += WidgetActiveTimerView_Loaded;
            // Se asocia el evento después de que cargue la vista inicial
            DesplegableFase.SelectionChanged += DesplegableFase_SelectionChanged;
        }

        private void WidgetActiveTimerView_Loaded(object sender, RoutedEventArgs e)
        {
            SincronizarDesdeBD();
        }

        private void ConfigurarBotonPausar(bool esPausar, bool habilitado)
        {
            BotonPausarReloj.IsEnabled = habilitado;

            if (!habilitado)
            {
                BotonPausarReloj.ClearValue(Button.BackgroundProperty);
                BotonPausarReloj.ClearValue(Button.ForegroundProperty);
                BotonPausarReloj.Content = "❚❚ PAUSAR";
                BotonPausarReloj.ToolTip = "Debes iniciar la fase antes de poder pausar o registrar tiempo";
                return;
            }

            if (esPausar)
            {
                BotonPausarReloj.Content = "❚❚ PAUSAR";
                BotonPausarReloj.Background = (Brush)new BrushConverter().ConvertFrom("#3B82F6")!;
                BotonPausarReloj.Foreground = Brushes.White;
                BotonPausarReloj.ToolTip = "Pausar el tiempo de la fase actual";
            }
            else
            {
                BotonPausarReloj.Content = "► REANUDAR";
                BotonPausarReloj.Background = (Brush)new BrushConverter().ConvertFrom("#10B981")!;
                BotonPausarReloj.Foreground = Brushes.White;
                BotonPausarReloj.ToolTip = "Reanudar el tiempo de esta fase";
            }
        }

        private void SincronizarDesdeBD()
        {
            var sesion = _sesionRepo.ObtenerSesion();

            if (sesion.ActividadId.HasValue)
            {
                var actividadRepo = new ActividadRepository();
                var actividad = actividadRepo.ObtenerPorId(sesion.ActividadId.Value);

                if (actividad != null && TextoNombreActividadActiva != null)
                {
                    TextoNombreActividadActiva.Text = actividad.Proyecto;
                }

                int faseActualId = sesion.FaseActualId ?? _registroRepo.ObtenerUltimaFasePorActividad(sesion.ActividadId.Value);

                if (!sesion.FaseActualId.HasValue)
                {
                    sesion.FaseActualId = faseActualId;
                    _sesionRepo.GuardarOSustituirSesion(sesion);
                }

                CargarDesplegableFases(faseActualId);
                _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);
                CargarInsigniasDefectosRaiz();
            }

            if (sesion.EstadoCronometro == 1 && sesion.FechaInicioSesion.HasValue)
            {
                _fechaInicioTramo = sesion.FechaInicioSesion.Value;
                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: true, habilitado: true);

                if (!_timerRelojUI.IsEnabled) _timerRelojUI.Start();
            }
            else
            {
                _timerRelojUI.Stop();
                _fechaInicioTramo = null;

                bool faseEnCurso = _segundosHistoricosOtros > 0;

                if (faseEnCurso)
                {
                    BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                    ConfigurarBotonPausar(esPausar: false, habilitado: true);
                }
                else
                {
                    BotonIniciarReloj.Content = "► INICIAR";
                    ConfigurarBotonPausar(esPausar: true, habilitado: false);
                }
            }

            ActualizarRelojPantalla();
        }

        private void CargarDesplegableFases(int faseActualId)
        {
            _isCargandoFases = true;
            var sesion = _sesionRepo.ObtenerSesion();
            var todasLasFases = _faseRepo.ObtenerTodas();
            List<int> fasesCompletadas = new List<int>();

            if (sesion.ActividadId.HasValue)
            {
                fasesCompletadas = _registroRepo.ObtenerFasesCompletadasPorActividad(sesion.ActividadId.Value);
            }

            _listaFasesUI = todasLasFases.Select(f => new FaseItemView
            {
                Id = f.Id,
                Nombre = f.Nombre,
                Orden = f.Orden,
                EsCompletada = fasesCompletadas.Contains(f.Id)
            }).ToList();

            DesplegableFase.ItemsSource = _listaFasesUI;
            DesplegableFase.SelectedValuePath = "Id";
            DesplegableFase.SelectedValue = faseActualId;

            _isCargandoFases = false;
        }

        private void DesplegableFase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isCargandoFases || DesplegableFase.SelectedValue == null) return;

            int nuevaFaseId = (int)DesplegableFase.SelectedValue;
            var sesion = _sesionRepo.ObtenerSesion();

            if (sesion.FaseActualId == nuevaFaseId) return;

            if (_fechaInicioTramo.HasValue || sesion.MinutosAcumulados > 0)
            {
                GuardarYLiquidarFaseActual(pausarCronometro: true);
            }

            _timerRelojUI.Stop();
            _fechaInicioTramo = null;

            sesion.FaseActualId = nuevaFaseId;
            sesion.EstadoCronometro = 0;
            sesion.FechaInicioSesion = null;
            sesion.MinutosAcumulados = 0;
            _sesionRepo.GuardarOSustituirSesion(sesion);

            BotonIniciarReloj.Content = "► INICIAR";
            BotonPausarReloj.Content = "❚❚ PAUSAR";
            ConfigurarBotonPausar(esPausar: true, habilitado: false); // Bloqueado hasta que se inicie

            ActualizarRelojPantalla();
        }

        private void BotonIniciarReloj_Click(object sender, RoutedEventArgs e)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            string textoBoton = BotonIniciarReloj.Content.ToString() ?? "";

            if (textoBoton.Contains("INICIAR"))
            {
                _fechaInicioTramo = DateTime.Now;
                sesion.EstadoCronometro = 1;
                sesion.FechaInicioSesion = _fechaInicioTramo;
                sesion.MinutosAcumulados = 0;
                sesion.UltimaActualizacion = DateTime.Now;

                _sesionRepo.GuardarOSustituirSesion(sesion);
                _timerRelojUI.Start();

                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: true, habilitado: true);
            }
            else
            {
                _timerRelojUI.Stop();
                GuardarYLiquidarFaseActual(pausarCronometro: true);

                int faseActualId = sesion.FaseActualId ?? 1;
                int indiceActual = _listaFasesUI.FindIndex(f => f.Id == faseActualId);
                int siguienteFaseId = faseActualId;

                // Avanzar a la siguiente fase
                if (indiceActual >= 0 && indiceActual < _listaFasesUI.Count - 1)
                {
                    siguienteFaseId = _listaFasesUI[indiceActual + 1].Id;
                }

                sesion.FaseActualId = siguienteFaseId;
                sesion.EstadoCronometro = 0;
                sesion.FechaInicioSesion = null;
                sesion.MinutosAcumulados = 0;
                _sesionRepo.GuardarOSustituirSesion(sesion);

                CargarDesplegableFases(siguienteFaseId);

                BotonIniciarReloj.Content = "► INICIAR";
                ConfigurarBotonPausar(esPausar: true, habilitado: false);
            }

            ActualizarRelojPantalla();
        }

        private void BotonPausarReloj_Click(object sender, RoutedEventArgs e)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            string textoBoton = BotonPausarReloj.Content.ToString() ?? "";

            if (textoBoton.Contains("PAUSAR"))
            {
                _inicioInterrupcion = DateTime.Now;
                _timerRelojUI.Stop();

                _ultimoRegistroEsfuerzoId = GuardarYLiquidarFaseActual(pausarCronometro: true);

                sesion.EstadoCronometro = 0;
                sesion.FechaInicioSesion = null;
                sesion.UltimaActualizacion = DateTime.Now;
                _sesionRepo.GuardarOSustituirSesion(sesion);

                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: false, habilitado: true); // Cambia a REANUDAR
            }
            else // REANUDAR
            {
                if (_inicioInterrupcion.HasValue && _ultimoRegistroEsfuerzoId > 0)
                {
                    int segundosInterrupcion = (int)(DateTime.Now - _inicioInterrupcion.Value).TotalSeconds;
                    if (segundosInterrupcion > 0)
                    {
                        _interrupcionRepo.Agregar(new Interrupcion
                        {
                            RegistroEsfuerzoId = (int)_ultimoRegistroEsfuerzoId,
                            DuracionMinutos = segundosInterrupcion,
                            FechaHora = DateTime.Now
                        });
                    }
                    _inicioInterrupcion = null;
                }

                _fechaInicioTramo = DateTime.Now;
                sesion.EstadoCronometro = 1;
                sesion.FechaInicioSesion = _fechaInicioTramo;

                // [REPARACIÓN CRUCIAL 1]: Reiniciamos esto a 0 para que no descarte registros en caso de fallo crítico
                sesion.MinutosAcumulados = 0;
                sesion.UltimaActualizacion = DateTime.Now;

                _sesionRepo.GuardarOSustituirSesion(sesion);
                _timerRelojUI.Start();

                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: true, habilitado: true); // Cambia a PAUSAR
            }

            ActualizarRelojPantalla();
        }

        private void TimerRelojUI_Tick(object? sender, EventArgs e)
        {
            if (_fechaInicioTramo.HasValue)
            {
                int segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
                var sesion = _sesionRepo.ObtenerSesion();

                if (segundosTramo > sesion.MinutosAcumulados)
                {
                    sesion.MinutosAcumulados = segundosTramo;
                    sesion.UltimaActualizacion = DateTime.Now;
                    _sesionRepo.GuardarOSustituirSesion(sesion);
                }
            }
            ActualizarRelojPantalla();
        }

        private long GuardarYLiquidarFaseActual(bool pausarCronometro)
        {
            if (!_fechaInicioTramo.HasValue) return 0;

            DateTime fechaInicio = _fechaInicioTramo.Value;
            DateTime fechaFin = DateTime.Now;
            int segundosTramoActual = (int)(fechaFin - fechaInicio).TotalSeconds;

            _fechaInicioTramo = null;

            if (segundosTramoActual <= 0) return 0;

            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue || !sesion.FaseActualId.HasValue) return 0;

            var nuevoRegistro = new RegistroEsfuerzo
            {
                ActividadId = sesion.ActividadId.Value,
                FaseId = sesion.FaseActualId.Value,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                MinutosEfectivos = segundosTramoActual
            };

            long idGenerado = _registroRepo.Agregar(nuevoRegistro);

            // [REPARACIÓN CRUCIAL 2]: Sincronizar en memoria de inmediato para que el reloj no "salte" o retroceda visualmente a 0
            _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);

            return idGenerado;
        }

        private void ActualizarRelojPantalla()
        {
            int segundosSesionActual = 0;

            if (_fechaInicioTramo.HasValue)
            {
                segundosSesionActual = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
            }

            int segundosTotales = _segundosHistoricosOtros + Math.Max(0, segundosSesionActual);
            TimeSpan tiempoTotal = TimeSpan.FromSeconds(segundosTotales);

            if (TextoRelojCronometro != null)
            {
                // [REPARACIÓN CRUCIAL 3]: Formateo seguro para más de 24 horas acumuladas de proyecto
                TextoRelojCronometro.Text = $"{(int)tiempoTotal.TotalHours:00}:{tiempoTotal.Minutes:00}:{tiempoTotal.Seconds:00}";
            }
        }

        private void BotonMinimizarWidget_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaMinimizada();
            }
        }

        private void BotonOpcionesWidget_Click(object sender, RoutedEventArgs e)
        {
            if (BotonOpcionesWidget.ContextMenu != null)
            {
                BotonOpcionesWidget.ContextMenu.IsOpen = true;
            }
        }

        private void MenuItemGuardarYSalir_Click(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();
            GuardarYLiquidarFaseActual(pausarCronometro: true);

            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.Close();
            }
        }

        private void MenuItemGuardarYLista_Click(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();
            GuardarYLiquidarFaseActual(pausarCronometro: true);

            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas();
            }
        }
        private void MenuItemFinalizarActividadWidget_Click(object sender, RoutedEventArgs e)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue) return;

            var actividadRepo = new ActividadRepository();
            var actividad = actividadRepo.ObtenerPorId(sesion.ActividadId.Value);
            if (actividad == null) return;

            var dialog = new Views.Dialogs.ConfirmarFinalizarWindow(actividad.Proyecto)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                GuardarYLiquidarFaseActual(pausarCronometro: true);
                actividadRepo.ActualizarEstado(actividad.Id, 1);

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.CargarListaProyectos();
                }

                if (Window.GetWindow(this) is WidgetWindow widgetWindow)
                {
                    widgetWindow.CargarVistaListaTareas();
                }
            }
        }

        private void BotonAgregarDefecto_Click(object sender, RoutedEventArgs e)
        {
            PausarYGuardarSesionPrincipal();

            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaDefecto(defectoIdParaCargar: null, defectoPadreId: null);
            }
        }

        #region Insignia de contador de defectos
        private void CargarInsigniasDefectosRaiz()
        {
            if (ContenedorInsigniasDefectos == null) return;
            ContenedorInsigniasDefectos.Children.Clear();

            var sesion = _sesionRepo.ObtenerSesion();
            if (!sesion.ActividadId.HasValue) return;

            var todosLosDefectos = _defectoRepo.ObtenerPorActividad(sesion.ActividadId.Value);
            var defectosRaiz = todosLosDefectos.Where(d => d.DefectoPadreId == null).ToList();

            for (int i = 0; i < defectosRaiz.Count; i++)
            {
                var defecto = defectosRaiz[i];
                int numeroDefecto = i + 1;

                string colorFondo = defecto.EsResuelto == 1 ? "#10B981" : "#FF3B30";
                string colorHover = defecto.EsResuelto == 1 ? "#059669" : "#E02D22";
                string estadoTexto = defecto.EsResuelto == 1 ? "Resuelto" : "Pendiente";

                TimeSpan tiempoTotal = TimeSpan.FromSeconds(defecto.TiempoCorreccionMinutos);
                string tiempoFormateado = $"{(int)tiempoTotal.TotalHours:00}:{tiempoTotal.Minutes:00}:{tiempoTotal.Seconds:00}";

                var btnBolita = CrearBotonBolita(numeroDefecto.ToString(), colorFondo, colorHover,
                    $"Defecto #{numeroDefecto} [{estadoTexto}]: {defecto.DescripcionError} ({tiempoFormateado})");

                btnBolita.Click += (s, e) =>
                {
                    PausarYGuardarSesionPrincipal();
                    if (Window.GetWindow(this) is WidgetWindow widget)
                    {
                        widget.CargarVistaDefecto(defectoIdParaCargar: defecto.Id);
                    }
                };

                ContenedorInsigniasDefectos.Children.Add(btnBolita);
            }
        }

        private Button CrearBotonBolita(string texto, string colorHex, string hoverHex, string tooltip)
        {
            var btn = new Button
            {
                Content = texto,
                Width = 30,
                Height = 30,
                Margin = new Thickness(4, 2, 4, 2),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
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

        private void PausarYGuardarSesionPrincipal()
        {
            _timerRelojUI.Stop();
            var sesion = _sesionRepo.ObtenerSesion();

            if (sesion.EstadoCronometro == 1)
            {
                GuardarYLiquidarFaseActual(pausarCronometro: true);

                sesion.EstadoCronometro = 0;
                sesion.FechaInicioSesion = null;
                sesion.UltimaActualizacion = DateTime.Now;
                _sesionRepo.GuardarOSustituirSesion(sesion);
            }

            _fechaInicioTramo = null;
        }
        #endregion
    }

    public class FaseItemView
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool EsCompletada { get; set; }
        public string IconoEstado => EsCompletada ? "✓" : "○";
        public string ColorEstado => EsCompletada ? "#10B981" : "#EF4444";
    }
}