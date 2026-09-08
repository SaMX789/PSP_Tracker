using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class WidgetCompactView : UserControl
    {
        private readonly IEstadoSesionRepository _sesionRepo;
        private readonly IRegistroEsfuerzoRepository _registroRepo;

        private readonly DispatcherTimer _timerRelojUI = new DispatcherTimer();
        private int _segundosHistoricosOtros = 0;
        private DateTime? _fechaInicioTramo;

        public WidgetCompactView()
        {
            InitializeComponent();
            _sesionRepo = new EstadoSesionRepository();
            _registroRepo = new RegistroEsfuerzoRepository();

            _timerRelojUI.Interval = TimeSpan.FromSeconds(1);
            _timerRelojUI.Tick += TimerRelojUI_Tick;

            Loaded += WidgetCompactView_Loaded;
            Unloaded += WidgetCompactView_Unloaded;
        }

        private void WidgetCompactView_Loaded(object sender, RoutedEventArgs e)
        {
            SincronizarDesdeBD();
        }

        private void WidgetCompactView_Unloaded(object sender, RoutedEventArgs e)
        {
            _timerRelojUI.Stop();
        }

        public void SincronizarDesdeBD()
        {
            var sesion = _sesionRepo.ObtenerSesion();

            if (sesion.ActividadId.HasValue)
            {
                var actividadRepo = new ActividadRepository();
                var actividad = actividadRepo.ObtenerPorId(sesion.ActividadId.Value);

                if (actividad != null && TextoNombreActividadMinimizada != null)
                {
                    TextoNombreActividadMinimizada.Text = actividad.Proyecto;
                }

                _segundosHistoricosOtros = _registroRepo.ObtenerMinutosTotalesPorActividad(sesion.ActividadId.Value);
            }

            // Si el temporizador venía corriendo en la vista completa, continúa contando sincrónicamente
            if (sesion.EstadoCronometro == 1 && sesion.FechaInicioSesion.HasValue)
            {
                _fechaInicioTramo = sesion.FechaInicioSesion.Value;
                if (!_timerRelojUI.IsEnabled) _timerRelojUI.Start();
            }
            else
            {
                _timerRelojUI.Stop();
                _fechaInicioTramo = null;
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

        private void ActualizarRelojPantalla()
        {
            int segundosTramo = 0;
            if (_fechaInicioTramo.HasValue)
            {
                segundosTramo = (int)(DateTime.Now - _fechaInicioTramo.Value).TotalSeconds;
            }

            int segundosTotales = _segundosHistoricosOtros + segundosTramo;
            TimeSpan tiempo = TimeSpan.FromSeconds(segundosTotales);

            if (TextoRelojMinimizado != null)
            {
                TextoRelojMinimizado.Text = tiempo.ToString(@"hh\:mm\:ss");
            }
        }
    }
}