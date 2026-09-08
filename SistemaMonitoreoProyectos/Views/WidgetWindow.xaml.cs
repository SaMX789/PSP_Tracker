using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Views.UserControls;
using System.Windows;
using System.Windows.Input;

namespace SistemaMonitoreoProyectos.Views
{
    public partial class WidgetWindow : Window
    {
        public WidgetWindow()
        {
            InitializeComponent();
            Loaded += WidgetWindow_Loaded;
            CargarVistaCrearTarea();
        }

        // Método público para cambiar la vista a el view para crear actividades
        public void CargarVistaCrearTarea()
        {
            ControlContenidoVista.Content = new WidgetCreateTaskView();
        }
        // Nuevo método para cargar la vista de lista de actividades antes creadas
        public void CargarVistaListaTareas()
        {
            ControlContenidoVista.Content = new WidgetTaskListView();
        }
        // Nuevo método para cargar en el widget vacio la nueva actividad creada
        public void CargarTimeActividadCreada()
        {
            ControlContenidoVista.Content = new WidgetActiveTimerView();
        }

        private void Ventana_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        #region MODO COMPACTO

        private bool _esModoCompacto = false;
        private object? _vistaAnterior;
        public void CargarVistaMinimizada()
        {
            // Guardar la vista del cronometro
            _vistaAnterior = ControlContenidoVista.Content;
            _esModoCompacto = true;

            // ocultar el marco de WidgetWindow 
            this.Width = 240;
            this.Height = 46;

            BordePrincipal.Background = System.Windows.Media.Brushes.Transparent;
            BordePrincipal.BorderBrush = System.Windows.Media.Brushes.Transparent;
            BordePrincipal.Padding = new Thickness(0);

            ControlContenidoVista.Content = new WidgetCompactView();
        }

        private void AjustarDimensionesModoNormal()
        {
            // Tamaño estándar para los formularios y cronómetro
            this.Width = 340;
            this.Height = 540;
            // Restaurar el fondo y borde estilo Dark Mode original de la ventana
            BordePrincipal.Background = (System.Windows.Media.Brush)FindResource("BrocheFondoVentana");
            BordePrincipal.BorderBrush = (System.Windows.Media.Brush)FindResource("BrocheBordeTarjeta");
            BordePrincipal.Padding = new Thickness(20);
            BordePrincipal.CornerRadius = new CornerRadius(20);
        }

        // Evento de doble clic en cualquier parte de la ventana
        private void Ventana_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Solo reacciona si el widget se encuentra minimizado
                if (_esModoCompacto)
                {
                    _esModoCompacto = false;
                    AjustarDimensionesModoNormal();

                    // Regresa exactamente a la vista del cronómetro que estaba abierta
                    ControlContenidoVista.Content = _vistaAnterior ?? new WidgetActiveTimerView();
                }
            }
        }
        #endregion

        #region RECUPERACIÓN DE TIEMPO POR ACCIDENTE DE CIERRE O CRASH DE LA APP
        private void WidgetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AutoRecuperarSesionSilenciosa();
        }

        private void AutoRecuperarSesionSilenciosa()
        {
            try
            {
                var sesionRepo = new EstadoSesionRepository();
                var sesion = sesionRepo.ObtenerSesion();

                // 1. Si la sesión venía activa tras el cierre abrupto, se pausa conservando los minutos guardados
                if (sesion.EstadoCronometro == 1)
                {
                    sesion.EstadoCronometro = 0;
                    sesion.FechaInicioSesion = null;
                    sesion.UltimaActualizacion = DateTime.Now;

                    sesionRepo.GuardarOSustituirSesion(sesion);
                }

                // 2. Si existe una tarea seleccionada en EstadoSesion, abre directamente el cronómetro con su progreso
                if (sesion.ActividadId.HasValue)
                {
                    CargarTimeActividadCreada();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AutoRecuperarSesionSilenciosa: {ex.Message}");
            }
        }
        #endregion
    }
}