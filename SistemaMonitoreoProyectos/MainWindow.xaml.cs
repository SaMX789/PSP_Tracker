using SistemaMonitoreoProyectos.Views;
using SistemaMonitoreoProyectos.Views.UserControls;
using System.Windows;

namespace SistemaMonitoreoProyectos
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Cargar la vista de Resumen (Overview) por defecto
            ControlContenidoDashboard.Content = new ActivityOverviewView();

            // Eventos de navegación entre pestañas
            BotonNavegarOverview.Click += (s, e) => ControlContenidoDashboard.Content = new ActivityOverviewView();
            BotonNavegarTimeLog.Click += (s, e) => ControlContenidoDashboard.Content = new TimeLogView();
            BotonNavegarDefectLog.Click += (s, e) => ControlContenidoDashboard.Content = new DefectLogView();

            
        }
                
        private void BotonAgregarNuevaActividad_Click_1(object sender, RoutedEventArgs e)
        {
            // 1. Buscar si ya existe una ventana activa del Widget
            WidgetWindow? widgetExistente = null;

            foreach (Window ventana in Application.Current.Windows)
            {
                if (ventana is WidgetWindow widget)
                {
                    widgetExistente = widget;
                    break;
                }
            }
            // 2. Si ya está abierto, cargamos la vista de creación y lo enfocamos
            if (widgetExistente != null)
            {
                widgetExistente.CargarVistaCrearTarea();
                if (widgetExistente.WindowState == WindowState.Minimized)
                {
                    widgetExistente.WindowState = WindowState.Normal;
                }
                widgetExistente.Activate();
                widgetExistente.Topmost = true; // Asegura que quede por encima
            }
            else
            {
                WidgetWindow nuevoWidget = new WidgetWindow();
                nuevoWidget.Show();
            }
        }
    }
}