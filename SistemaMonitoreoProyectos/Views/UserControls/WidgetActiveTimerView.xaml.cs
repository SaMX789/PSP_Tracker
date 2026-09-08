using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using System;
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
        public WidgetActiveTimerView()
        {
            InitializeComponent();
            _sesionRepo = new EstadoSesionRepository();
            _registroRepo = new RegistroEsfuerzoRepository();
            _faseRepo = new FaseRepository();
            _timerRelojUI.Interval = TimeSpan.FromSeconds(1);
            _timerRelojUI.Tick += TimerRelojUI_Tick;
            Loaded += WidgetActiveTimerView_Loaded;
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
                // Restablece el color base del tema cuando está deshabilitado
                BotonPausarReloj.ClearValue(Button.BackgroundProperty);
                BotonPausarReloj.ClearValue(Button.ForegroundProperty);
                BotonPausarReloj.Content = "❚❚ PAUSAR";
                BotonPausarReloj.ToolTip = "Debes iniciar la fase antes de poder pausar o registrar tiempo";
                return;
            }

            if (esPausar)
            {
                // Azulito para PAUSAR
                BotonPausarReloj.Content = "❚❚ PAUSAR";
                BotonPausarReloj.Background = (Brush)new BrushConverter().ConvertFrom("#3B82F6")!;
                BotonPausarReloj.Foreground = Brushes.White;
                BotonPausarReloj.ToolTip = "Pausar el tiempo de la fase actual";
            }
            else
            {
                // Verde para REANUDAR
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
            }

            // 1. ESTADO CORRIENDO
            if (sesion.EstadoCronometro == 1 && sesion.FechaInicioSesion.HasValue)
            {
                _fechaInicioTramo = sesion.FechaInicioSesion.Value;
                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: true, habilitado: true); // Azulito

                if (!_timerRelojUI.IsEnabled) _timerRelojUI.Start();
            }
            // 2. ESTADO DETENIDO / PAUSADO
            else
            {
                _timerRelojUI.Stop();
                _fechaInicioTramo = null;

                bool faseEnCurso = false;
                if (sesion.ActividadId.HasValue && sesion.FaseActualId.HasValue)
                {
                    int segundosEnFaseActual = _registroRepo.ObtenerSegundosPorFase(sesion.ActividadId.Value, sesion.FaseActualId.Value);
                    faseEnCurso = segundosEnFaseActual > 0;
                }

                if (faseEnCurso)
                {
                    // ESTADO PAUSADO
                    BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                    ConfigurarBotonPausar(esPausar: false, habilitado: true); // Verde (REANUDAR)
                }
                else
                {
                    // FASE NUEVA / SIN INICIAR
                    BotonIniciarReloj.Content = "► INICIAR";
                    ConfigurarBotonPausar(esPausar: true, habilitado: false); // Deshabilitado
                }
            }

            ActualizarRelojPantalla();
        }

        private List<FaseItemView> _listaFasesUI = new List<FaseItemView>();

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

            // Mapear catálogo a objetos de vista con indicador de color
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

            // 1. Si el reloj estaba corriendo, liquidamos el tiempo consumido hasta el segundo exacto
            if (_fechaInicioTramo.HasValue || sesion.MinutosAcumulados > 0)
            {
                GuardarYLiquidarFaseActual(pausarCronometro: true);
            }

            // 2. Detener el reloj para la nueva fase
            _timerRelojUI.Stop();
            _fechaInicioTramo = null;

            // 3. Registrar la nueva fase en EstadoSesion en estado detenido (0)
            sesion.FaseActualId = nuevaFaseId;
            sesion.EstadoCronometro = 0;
            sesion.FechaInicioSesion = null;
            sesion.MinutosAcumulados = 0;
            _sesionRepo.GuardarOSustituirSesion(sesion);

            // 4. Resetear los botones visuales a estado listo
            BotonIniciarReloj.Content = "► INICIAR";
            BotonPausarReloj.Content = "❚❚ PAUSAR";

            // 5. Refrescar tiempo histórico y reloj visual en pantalla
            if (sesion.ActividadId.HasValue)
            {
                _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);
            }

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
                ConfigurarBotonPausar(esPausar: true, habilitado: true); // Cambia a Azulito (PAUSAR)
            }
            else
            {
                _timerRelojUI.Stop();
                GuardarYLiquidarFaseActual(pausarCronometro: true);

                int faseActualId = sesion.FaseActualId ?? 1;
                int indiceActual = _listaFasesUI.FindIndex(f => f.Id == faseActualId);
                int siguienteFaseId = faseActualId;

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
                ConfigurarBotonPausar(esPausar: true, habilitado: false); // Se bloquea en la nueva fase
            }

            ActualizarRelojPantalla();
        }

        private DateTime? _inicioInterrupcion;
        
        private readonly IInterrupcionRepository _interrupcionRepo = new InterrupcionRepository();
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
                ConfigurarBotonPausar(esPausar: false, habilitado: true); // Cambia a Verde (REANUDAR)
            }
            else
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
                sesion.UltimaActualizacion = DateTime.Now;

                _sesionRepo.GuardarOSustituirSesion(sesion);
                _timerRelojUI.Start();

                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                ConfigurarBotonPausar(esPausar: true, habilitado: true); // Cambia a Azulito (PAUSAR)
            }

            ActualizarRelojPantalla();
        }

        private void TimerRelojUI_Tick(object? sender, EventArgs e)
        {
            if (_fechaInicioTramo.HasValue)
            {
                int segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;

                var sesion = _sesionRepo.ObtenerSesion();
                // Respaldar segundos en EstadoSesion sin truncar
                if (segundosTramo > sesion.MinutosAcumulados)
                {
                    sesion.MinutosAcumulados = segundosTramo; 
                    sesion.UltimaActualizacion = DateTime.Now;
                    _sesionRepo.GuardarOSustituirSesion(sesion);
                }
            }

            ActualizarRelojPantalla();
        }
        
        private long _ultimoRegistroEsfuerzoId = 0;
        private long GuardarYLiquidarFaseActual(bool pausarCronometro)
        {
            var sesion = _sesionRepo.ObtenerSesion();
            long idGenerado = 0;

            int segundosTramo = 0;
            if (_fechaInicioTramo.HasValue)
            {
                segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
            }

            int segundosEfectivosTramo = Math.Max(segundosTramo, sesion.MinutosAcumulados);

            if (sesion.ActividadId.HasValue && segundosEfectivosTramo > 0)
            {
                idGenerado = _registroRepo.Agregar(new RegistroEsfuerzo
                {
                    ActividadId = sesion.ActividadId.Value,
                    FaseId = sesion.FaseActualId ?? 1,
                    MinutosEfectivos = segundosEfectivosTramo,
                    FechaInicio = sesion.FechaInicioSesion ?? DateTime.Now.AddSeconds(-segundosEfectivosTramo),
                    FechaFin = DateTime.Now
                });

                _ultimoRegistroEsfuerzoId = idGenerado;
            }

            sesion.MinutosAcumulados = 0;

            if (pausarCronometro)
            {
                sesion.FechaInicioSesion = null;
                _fechaInicioTramo = null;
            }
            else if (sesion.EstadoCronometro == 1)
            {
                _fechaInicioTramo = DateTime.Now;
                sesion.FechaInicioSesion = _fechaInicioTramo;
            }

            sesion.UltimaActualizacion = DateTime.Now;
            _sesionRepo.GuardarOSustituirSesion(sesion);

            if (sesion.ActividadId.HasValue)
            {
                _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);
            }

            return idGenerado;
        }

        private void ActualizarRelojPantalla()
        {
            int segundosTramo = 0;

            if (_fechaInicioTramo.HasValue)
            {
                segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
            }

            // El tiempo en vivo es estrictamente: Histórico previo de la actividad + Segundos del tramo actual
            int segundosTotales = _segundosHistoricosOtros + segundosTramo;

            TimeSpan tiempo = TimeSpan.FromSeconds(segundosTotales);
            if (TextoRelojCronometro != null)
            {
                TextoRelojCronometro.Text = tiempo.ToString(@"hh\:mm\:ss");
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
        // 1. Guarda la sesión actual y cierra el widget por completo
        private void MenuItemGuardarYSalir_Click(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();
            GuardarYLiquidarFaseActual(pausarCronometro: true);

            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.Close();
            }
        }

        // 2. Guarda la sesión actual y regresa a la vista del catálogo de tareas
        private void MenuItemGuardarYLista_Click(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();
            GuardarYLiquidarFaseActual(pausarCronometro: true);

            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas(); // Cambia a la lista de tareas dentro del widget
            }
        }
    }
    public class FaseItemView
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool EsCompletada { get; set; }

        public string IconoEstado => EsCompletada ? "✓" : "○";
        public string ColorEstado => EsCompletada ? "#10B981" : "#EF4444"; // Verde / Rojo
    }
}