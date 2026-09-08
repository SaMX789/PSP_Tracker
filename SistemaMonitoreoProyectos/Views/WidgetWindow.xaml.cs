using System;
using System.Windows;
using System.Windows.Input;
using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Views.UserControls;

namespace SistemaMonitoreoProyectos.Views
{
    public partial class WidgetWindow : Window
    {
        private bool _esModoCompacto = false;
        private object? _vistaAnterior;

        public WidgetWindow()
        {
            InitializeComponent();
            Loaded += WidgetWindow_Loaded;
            CargarVistaCrearTarea();
        }

        // Método público para cambiar a la vista de creación de actividades
        public void CargarVistaCrearTarea()
        {
            AjustarDimensionesModoNormal();
            ControlContenidoVista.Content = new WidgetCreateTaskView();
        }

        // Método para cargar la vista de lista de actividades
        public void CargarVistaListaTareas()
        {
            AjustarDimensionesModoNormal();
            ControlContenidoVista.Content = new WidgetTaskListView();
        }

        // Método para cargar el cronómetro de la actividad
        public void CargarTimeActividadCreada()
        {
            AjustarDimensionesModoNormal();
            ControlContenidoVista.Content = new WidgetActiveTimerView();
        }

        // Manejador unificado de clic y doble clic para evitar conflictos con DragMove()
        private void Ventana_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // 1. Detectar DOBLE CLIC estando en modo compacto
                if (e.ClickCount == 2 && _esModoCompacto)
                {
                    RestaurarModoNormal();
                    return;
                }

                // 2. Si es un solo clic/arrastre, mover la ventana
                this.DragMove();
            }
        }

        #region MODO COMPACTO

        public void CargarVistaMinimizada()
        {
            // Guardar la vista activa previa
            _vistaAnterior = ControlContenidoVista.Content;
            _esModoCompacto = true;

            // Ajustar el marco a la píldora compacta
            this.Width = 240;
            this.Height = 46;

            BordePrincipal.Background = System.Windows.Media.Brushes.Transparent;
            BordePrincipal.BorderBrush = System.Windows.Media.Brushes.Transparent;
            BordePrincipal.Padding = new Thickness(0);

            ControlContenidoVista.Content = new WidgetCompactView();
        }

        public void RestaurarModoNormal()
        {
            if (!_esModoCompacto) return;

            AjustarDimensionesModoNormal();

            // Regresa exactamente a la vista del cronómetro que estaba abierta
            ControlContenidoVista.Content = _vistaAnterior ?? new WidgetActiveTimerView();
        }

        private void AjustarDimensionesModoNormal()
        {
            _esModoCompacto = false;

            // Tamaño estándar para los formularios y cronómetro
            this.Width = 340;
            this.Height = 540;

            // Restaurar el fondo y borde estilo Dark Mode original de la ventana
            BordePrincipal.Background = (System.Windows.Media.Brush)FindResource("BrocheFondoVentana");
            BordePrincipal.BorderBrush = (System.Windows.Media.Brush)FindResource("BrocheBordeTarjeta");
            BordePrincipal.Padding = new Thickness(20);
            BordePrincipal.CornerRadius = new CornerRadius(20);
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

                // Si la sesión venía activa tras el cierre abrupto, se pausa conservando los minutos guardados
                if (sesion.EstadoCronometro == 1)
                {
                    sesion.EstadoCronometro = 0;
                    sesion.FechaInicioSesion = null;
                    sesion.UltimaActualizacion = DateTime.Now;

                    sesionRepo.GuardarOSustituirSesion(sesion);
                }

                // Si existe una tarea seleccionada en EstadoSesion, abre directamente el cronómetro con su progreso
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