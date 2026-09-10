using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Views;
using SistemaMonitoreoProyectos.Views.UserControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaMonitoreoProyectos
{
    public partial class MainWindow : Window
    {
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private int _actividadSeleccionadaId = 0;
        private string _tabActual = "Overview";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => CargarListaProyectos();

            BotonNavegarOverview.Click += (s, e) => CambiarTab("Overview");
            BotonNavegarTimeLog.Click += (s, e) => CambiarTab("TimeLog");
            BotonNavegarDefectLog.Click += (s, e) => CambiarTab("DefectLog");
        }

        public void CargarListaProyectos()
        {
            ContenedorProyectosActivos.Children.Clear();
            var actividades = _actividadRepo.ObtenerTodas();

            if (actividades.Count > 0 && _actividadSeleccionadaId == 0)
            {
                _actividadSeleccionadaId = actividades[0].Id;
            }

            foreach (var act in actividades)
            {
                bool esSeleccionada = act.Id == _actividadSeleccionadaId;

                var border = new Border
                {
                    Style = (Style)FindResource("EstiloTarjetaBase"),
                    Background = esSeleccionada ? (Brush)new BrushConverter().ConvertFrom("#283244")! : (Brush)new BrushConverter().ConvertFrom("#1E1E22")!,
                    BorderBrush = esSeleccionada ? (Brush)FindResource("BrocheAcentoPrimario") : (Brush)FindResource("BrocheBordeTarjeta"),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(12),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal };

                var icon = new TextBlock
                {
                    Text = act.Estado == 1 ? "\uE73E " : "\uE943 ",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    Foreground = (Brush)FindResource("BrocheAcentoPrimario"),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var text = new TextBlock
                {
                    Text = act.Proyecto,
                    Foreground = (Brush)FindResource("BrocheTextoPrincipal"),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                sp.Children.Add(icon);
                sp.Children.Add(text);
                border.Child = sp;

                border.MouseLeftButtonDown += (s, e) =>
                {
                    _actividadSeleccionadaId = act.Id;
                    CargarListaProyectos();
                    RenderizarVistaActual();
                };

                ContenedorProyectosActivos.Children.Add(border);
            }

            RenderizarVistaActual();
        }

        private void CambiarTab(string tab)
        {
            _tabActual = tab;
            RenderizarVistaActual();
        }

        private void RenderizarVistaActual()
        {
            if (_actividadSeleccionadaId == 0) return;

            switch (_tabActual)
            {
                case "Overview":
                    var overview = new ActivityOverviewView();
                    overview.CargarMetricasActividad(_actividadSeleccionadaId);
                    ControlContenidoDashboard.Content = overview;
                    break;
                case "TimeLog":
                    var timeLog = new TimeLogView();
                    timeLog.CargarRegistrosTiempo(_actividadSeleccionadaId);
                    ControlContenidoDashboard.Content = timeLog;
                    break;
                case "DefectLog":
                    var defectLog = new DefectLogView();
                    defectLog.CargarBitacoraDefectos(_actividadSeleccionadaId);
                    ControlContenidoDashboard.Content = defectLog;
                    break;
            }
        }

        private void BotonAgregarNuevaActividad_Click_1(object sender, RoutedEventArgs e)
        {
            WidgetWindow nuevoWidget = new WidgetWindow();
            nuevoWidget.Show();
        }
    }
}