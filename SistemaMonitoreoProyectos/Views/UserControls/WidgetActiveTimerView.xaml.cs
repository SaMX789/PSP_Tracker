using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

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
                int faseActualId = sesion.FaseActualId ?? 1;
                CargarDesplegableFases(faseActualId);
                _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);
            }
            if (sesion.EstadoCronometro == 1 && sesion.FechaInicioSesion.HasValue)
            {
                _fechaInicioTramo = sesion.FechaInicioSesion.Value;
                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                BotonPausarReloj.Content = "❚❚ PAUSAR";
                if (!_timerRelojUI.IsEnabled) _timerRelojUI.Start();
            }
            else
            {
                _timerRelojUI.Stop();
                int totalSegundos = _segundosHistoricosOtros + sesion.MinutosAcumulados;
                if (totalSegundos > 0)
                {
                    BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                    BotonPausarReloj.Content = "► REANUDAR";
                }
                else
                {
                    BotonIniciarReloj.Content = "► INICIAR";
                    BotonPausarReloj.Content = "❚❚ PAUSAR";
                }
            }
            ActualizarRelojPantalla();
        }
        private void CargarDesplegableFases(int faseActualId)
        {
            _isCargandoFases = true;
            var fases = _faseRepo.ObtenerTodas();
            DesplegableFase.ItemsSource = fases;
            DesplegableFase.DisplayMemberPath = "Nombre";
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

            // 1. Guardar SEGUNDOS exactos trabajados en la fase anterior
            GuardarYLiquidarFaseActual(pausarCronometro: false);

            // 2. Asignar la nueva fase
            sesion.FaseActualId = nuevaFaseId;
            _sesionRepo.GuardarOSustituirSesion(sesion);

            // 3. Continuar la visualización del tiempo total sin reseteos visuales
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
                sesion.UltimaActualizacion = DateTime.Now;

                _sesionRepo.GuardarOSustituirSesion(sesion);
                _timerRelojUI.Start();

                BotonIniciarReloj.Content = "⏹ TERMINAR FASE";
                BotonPausarReloj.Content = "❚❚ PAUSAR";
            }
            else
            {
                _timerRelojUI.Stop();
                GuardarYLiquidarFaseActual(pausarCronometro: true);

                BotonIniciarReloj.Content = "► INICIAR";
                BotonPausarReloj.Content = "❚❚ PAUSAR";
            }
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

                // Al pausar, se liquida el bloque de trabajo y se obtiene su RegistroEsfuerzoId
                _ultimoRegistroEsfuerzoId = GuardarYLiquidarFaseActual(pausarCronometro: true);

                BotonPausarReloj.Content = "► REANUDAR";
            }
            else
            {
                // Al reanudar, si existe un bloque previo, registramos la interrupción vinculada a RegistroEsfuerzoId
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

                BotonPausarReloj.Content = "❚❚ PAUSAR";
            }
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

            int segundosEfectivosTramo = sesion.MinutosAcumulados > segundosTramo ? sesion.MinutosAcumulados : segundosTramo;

            if (sesion.ActividadId.HasValue && segundosEfectivosTramo > 0)
            {
                // 'Agregar' devuelve 'long', por lo que idGenerado debe ser 'long' (o usar casting explícito)
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
                sesion.EstadoCronometro = 0;
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
            var sesion = _sesionRepo.ObtenerSesion();
            int segundosTramo = 0;

            if (_fechaInicioTramo.HasValue)
            {
                segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
            }

            int segundosTotales = _segundosHistoricosOtros + (sesion.MinutosAcumulados > segundosTramo ? sesion.MinutosAcumulados : segundosTramo);

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
        private void MenuItemGuardarYSalir_Click(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();

            // 1. Guardar el progreso de la fase actual en RegistrosEsfuerzo
            GuardarYLiquidarFaseActual(pausarCronometro: true);

            // 2. Volver a la pantalla principal de lista de tareas
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas(); // O el método para volver al listado
            }
        }
    }
}