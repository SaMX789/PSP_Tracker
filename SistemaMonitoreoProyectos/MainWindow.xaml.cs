using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Views;
using SistemaMonitoreoProyectos.Views.UserControls;

namespace SistemaMonitoreoProyectos
{
    public partial class MainWindow : Window
    {
        // Importación de API de Windows para forzar el título en Modo Oscuro Nativo
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private int _actividadSeleccionadaId = 0;
        private string _tabActual = "Overview";

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += (s, e) => CargarListaProyectos();

            BotonNavegarOverview.Click += (s, e) => CambiarTab("Overview");
            BotonNavegarTimeLog.Click += (s, e) => CambiarTab("TimeLog");
            BotonNavegarDefectLog.Click += (s, e) => CambiarTab("DefectLog");
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Activa la barra de título oscura nativa manteniendo los botones de Windows y el resize normal
            var helper = new WindowInteropHelper(this);
            int darkMode = 1; // 1 = True
            if (DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }
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
                    Text = act.Estado == 1 ? "\uE73E " : "\uE916 ",
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
            Close();
        }
    }
}