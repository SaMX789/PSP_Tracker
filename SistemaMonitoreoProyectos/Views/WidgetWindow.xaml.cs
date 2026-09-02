using System.Windows;
using System.Windows.Input;
using SistemaMonitoreoProyectos.Views.UserControls;

namespace SistemaMonitoreoProyectos.Views
{
    public partial class WidgetWindow : Window
    {
        public WidgetWindow()
        {
            InitializeComponent();
            CargarVistaCrearTarea();
        }

        // Método público para cambiar la vista desde MainWindow o controllers
        public void CargarVistaCrearTarea()
        {
            ControlContenidoVista.Content = new WidgetCreateTaskView();
        }

        // Nuevo método para cargar la vista de lista de actividades
        public void CargarVistaListaTareas()
        {
            ControlContenidoVista.Content = new WidgetTaskListView();
        }
        // Nuevo método para cargar la vista de lista de actividades
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
    }
}