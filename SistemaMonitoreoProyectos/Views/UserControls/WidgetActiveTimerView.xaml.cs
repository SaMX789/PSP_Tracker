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
    /// Lógica de interacción para WidgetActiveTimerView.xaml
    /// </summary>
    public partial class WidgetActiveTimerView : UserControl
    {
        public WidgetActiveTimerView()
        {
            InitializeComponent();
        }

        private void BotonMinimizarWidget_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaMinimizada();
            }
        }
    }
}
