using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Reports;
using SistemaMonitoreoProyectos.Views;
using SistemaMonitoreoProyectos.Views.UserControls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Linq;

namespace SistemaMonitoreoProyectos
{
    public partial class MainWindow : Window
    {
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

            // Configurar licencia Community de QuestPDF (Gratuito y legal)
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;

            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += (s, e) => CargarListaProyectos();

            BotonNavegarOverview.Click += (s, e) => CambiarTab("Overview");
            BotonNavegarTimeLog.Click += (s, e) => CambiarTab("TimeLog");
            BotonNavegarDefectLog.Click += (s, e) => CambiarTab("DefectLog");

            // Asignar evento de exportación PDF
            BotonConfiguracion.Click += BotonConfiguracion_Click;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            int darkMode = 1;
            if (DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }
        }

        private void BotonConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            if (_actividadSeleccionadaId == 0)
            {
                MessageBox.Show("Selecciona una actividad para exportar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actividad = _actividadRepo.ObtenerPorId(_actividadSeleccionadaId);
            if (actividad == null) return;

            var planRepo = new PlanFaseRepository();
            var esfuerzoRepo = new RegistroEsfuerzoRepository();
            var defectoRepo = new RegistroDefectoRepository();
            var faseRepo = new FaseRepository();

            var fasesCatalog = faseRepo.ObtenerTodas().OrderBy(f => f.Orden).ToList();
            var planes = planRepo.ObtenerPorActividad(actividad.Id);
            var esfuerzos = esfuerzoRepo.ObtenerPorActividad(actividad.Id);
            var defectos = defectoRepo.ObtenerPorActividad(actividad.Id);

            var ordenFases = fasesCatalog.ToDictionary(f => (long)f.Id, f => f.Orden);

            // 1. Recopilar métricas globales corrigiendo conversión de BD (Segundos -> Minutos)
            int tiempoEstimadoMin = planes != null && planes.Count > 0 ? planes.Sum(p => p.TiempoEstimadoMinutos) : 0;
            int tiempoRealMin = esfuerzos != null ? (int)Math.Round(esfuerzos.Sum(e => e.MinutosEfectivos) / 60.0) : 0;
            int tiempoRetrabajoMin = defectos != null ? (int)Math.Round(defectos.Sum(d => d.TiempoCorreccionMinutos) / 60.0) : 0;

            int defectosFugaSevera = defectos != null ? defectos.Count(d =>
            {
                int ordOrigen = ordenFases.ContainsKey(d.FaseOrigenId) ? ordenFases[d.FaseOrigenId] : 0;
                int ordDeteccion = ordenFases.ContainsKey(d.FaseDeteccionId) ? ordenFases[d.FaseDeteccionId] : 0;
                return (ordDeteccion - ordOrigen) >= 2;
            }) : 0;

            // 2. Armar Lista de Desglose de Fases (Línea Base Histórica)
            var listaFasesReporte = new System.Collections.Generic.List<FaseReporteDTO>();
            foreach (var fase in fasesCatalog)
            {
                int estMin = planes?.FirstOrDefault(p => p.FaseId == fase.Id)?.TiempoEstimadoMinutos ?? 0;
                int realSeg = esfuerzos?.Where(e => e.FaseId == fase.Id).Sum(e => e.MinutosEfectivos) ?? 0;
                int realMin = (int)Math.Round(realSeg / 60.0);

                // Solo mostrar la fase en la tabla si hubo plan o si hubo esfuerzo real registrado
                if (estMin > 0 || realMin > 0)
                {
                    listaFasesReporte.Add(new FaseReporteDTO
                    {
                        NombreFase = fase.Nombre,
                        MinutosEstimados = estMin,
                        MinutosReales = realMin
                    });
                }
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_LineaBase_{actividad.Proyecto.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf",
                Title = "Guardar Reporte Línea Base PSP"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var metricasDto = new ReporteMetricsDTO
                    {
                        ActividadId = actividad.Id,
                        Proyecto = actividad.Proyecto,
                        Responsable = actividad.Responsable,
                        EsCompletado = actividad.Estado == 1,
                        TiempoEstimadoMinutos = tiempoEstimadoMin,
                        TiempoRealMinutos = tiempoRealMin,
                        TiempoRetrabajoMinutos = tiempoRetrabajoMin,
                        TotalDefectos = defectos?.Count ?? 0,
                        DefectosFugaSeveraCount = defectosFugaSevera,
                        FasesDetalle = listaFasesReporte // Pasamos el array de fases al reporte
                    };

                    var documento = new ReporteActividadDocument(metricasDto);
                    documento.GeneratePdf(saveFileDialog.FileName);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    Cursor = Cursors.Hand
                };

                // --- AGREGAR MENÚ CONTEXTUAL PARA CLIC DERECHO ---
                var contextMenu = new ContextMenu();
                var menuEliminar = new MenuItem
                {
                    Header = "🗑 Eliminar Actividad",
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#FF4D4D")!
                };

                menuEliminar.Click += (s, e) => EliminarActividadConRegla(act.Id, act.Proyecto);
                contextMenu.Items.Add(menuEliminar);
                border.ContextMenu = contextMenu;
                // ------------------------------------------------

                var sp = new StackPanel { Orientation = Orientation.Horizontal };

                var icon = new TextBlock
                {
                    Text = act.Estado == 1 ? "\uE73E " : "\uE916 ",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    Foreground = (Brush)FindResource("BrocheAcentoPrimario"),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center 
                };

                var text = new TextBlock
                {
                    Text = act.Proyecto,
                    Foreground = (Brush)FindResource("BrocheTextoPrincipal"),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center 
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
        private void EliminarActividadConRegla(int actividadId, string nombreProyecto)
        {
            var esfuerzoRepo = new RegistroEsfuerzoRepository();
            int totalSegundos = esfuerzoRepo.ObtenerMinutosTotalesPorActividad(actividadId);

            if (totalSegundos < 600) // Menos de 10 minutos
            {
                var result = MessageBox.Show(
                    $"¿Deseas eliminar permanentemente '{nombreProyecto}'?\n\nAl tener menos de 10 minutos de esfuerzo registrado, no afecta la línea base.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Llamada limpia al repositorio
                    _actividadRepo.Eliminar(actividadId);

                    if (_actividadSeleccionadaId == actividadId)
                        _actividadSeleccionadaId = 0;

                    CargarListaProyectos();
                }
            }
            else
            {
                MessageBox.Show(
                    $"La actividad '{nombreProyecto}' contiene más de 10 minutos de registros históricos.\n\nPara proteger la integridad de la línea base gerencial, no se permite su eliminación permanente.",
                    "Protección de Datos Históricos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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

        private void BotonExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            if (_actividadSeleccionadaId == 0)
            {
                MessageBox.Show("Selecciona una actividad para exportar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actividad = _actividadRepo.ObtenerPorId(_actividadSeleccionadaId);
            if (actividad == null) return;

            var planRepo = new PlanFaseRepository();
            var esfuerzoRepo = new RegistroEsfuerzoRepository();
            var defectoRepo = new RegistroDefectoRepository();
            var faseRepo = new FaseRepository();

            var fasesCatalog = faseRepo.ObtenerTodas().OrderBy(f => f.Orden).ToList();
            var planes = planRepo.ObtenerPorActividad(actividad.Id);
            var esfuerzos = esfuerzoRepo.ObtenerPorActividad(actividad.Id);
            var defectos = defectoRepo.ObtenerPorActividad(actividad.Id);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Datos_PSP_{actividad.Proyecto.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Exportar Datos de Actividad a CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();

                    // 1. Resumen Ejecutivo Global
                    sb.AppendLine("ID_Actividad;Proyecto;Responsable;Estado;Tiempo_Estimado_Min;Tiempo_Real_Min;Tiempo_Retrabajo_Min;Defectos_Totales;Fugas_Severas;Factor_Calibracion");

                    int estMinTotal = planes != null && planes.Count > 0 ? planes.Sum(p => p.TiempoEstimadoMinutos) : 0;
                    int realMinTotal = esfuerzos != null ? (int)Math.Round(esfuerzos.Sum(e => e.MinutosEfectivos) / 60.0) : 0;
                    int retrabajoMinTotal = defectos != null ? (int)Math.Round(defectos.Sum(d => d.TiempoCorreccionMinutos) / 60.0) : 0;
                    int totalDefectos = defectos?.Count ?? 0;

                    var ordenFases = fasesCatalog.ToDictionary(f => (long)f.Id, f => f.Orden);
                    int fugasSeveras = defectos != null ? defectos.Count(d =>
                    {
                        int ordOrig = ordenFases.ContainsKey(d.FaseOrigenId) ? ordenFases[d.FaseOrigenId] : 0;
                        int ordDet = ordenFases.ContainsKey(d.FaseDeteccionId) ? ordenFases[d.FaseDeteccionId] : 0;
                        return (ordDet - ordOrig) >= 2;
                    }) : 0;

                    double factorCalib = estMinTotal > 0 ? (double)realMinTotal / estMinTotal : 1.0;
                    string estado = actividad.Estado == 1 ? "Completado" : "En Curso";

                    sb.AppendLine($"{actividad.Id};\"{actividad.Proyecto}\";\"{actividad.Responsable}\";{estado};{estMinTotal};{realMinTotal};{retrabajoMinTotal};{totalDefectos};{fugasSeveras};{factorCalib:0.00}");

                    // 2. Separador de Sección
                    sb.AppendLine();
                    sb.AppendLine("DESGLOSE POR FASES");
                    sb.AppendLine("Fase;Tiempo_Estimado_Min;Tiempo_Real_Min;Desviacion_Min");

                    // 3. Filas de Desglose
                    foreach (var fase in fasesCatalog)
                    {
                        int estMin = planes?.FirstOrDefault(p => p.FaseId == fase.Id)?.TiempoEstimadoMinutos ?? 0;
                        int realSeg = esfuerzos?.Where(e => e.FaseId == fase.Id).Sum(e => e.MinutosEfectivos) ?? 0;
                        int realMin = (int)Math.Round(realSeg / 60.0);
                        int desvMin = realMin - estMin;

                        if (estMin > 0 || realMin > 0)
                        {
                            sb.AppendLine($"\"{fase.Nombre}\";{estMin};{realMin};{desvMin}");
                        }
                    }

                    // Guardar con codificación UTF-8 con BOM para correcta apertura directa en Microsoft Excel
                    System.IO.File.WriteAllText(saveFileDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);

                    MessageBox.Show("Archivo CSV generado exitosamente.", "Exportación Completa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar a CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BotonBackup_Click(object sender, RoutedEventArgs e)
        {
            var backupModal = new SistemaMonitoreoProyectos.Views.Dialogs.BackupWindow
            {
                Owner = this
            };

            if (backupModal.ShowDialog() == true)
            {
                CargarListaProyectos(); // Recarga las tarjetas si se importó un proyecto nuevo
            }
        }
    }
}