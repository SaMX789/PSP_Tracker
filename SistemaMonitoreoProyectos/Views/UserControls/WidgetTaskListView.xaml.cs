using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    /// <summary>
    /// Lógica de interacción para WidgetTaskListView.xaml
    /// </summary>
    public partial class WidgetTaskListView : UserControl
    {
        public WidgetTaskListView()
        {
            InitializeComponent();
        }

        private void BotonFlotanteNuevaTarea_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la ventana contenedora (WidgetWindow) y cambiar la vista
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaCrearTarea();
            }
        }
    }
}
