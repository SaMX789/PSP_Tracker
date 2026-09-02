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
    /// Lógica de interacción para WidgetCreateTaskView.xaml
    /// </summary>
    public partial class WidgetCreateTaskView : UserControl
    {
        public WidgetCreateTaskView()
        {
            InitializeComponent();
            // Vincular el clic del botón "Continuar con una actividad ya creada"
            BotonCargarExistente.Click += BotonCargarExistente_Click;
        }

        private void BotonCargarExistente_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la ventana contenedora (WidgetWindow) y cambiar la vista
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas();
            }
        }

        private void BotonSiguiente_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la ventana contenedora (WidgetWindow) y cambiar la vista
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarTimeActividadCreada();
            }
        }
    }
}
