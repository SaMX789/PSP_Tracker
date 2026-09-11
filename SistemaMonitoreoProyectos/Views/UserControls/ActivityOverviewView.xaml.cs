using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class ActivityOverviewView : UserControl
    {
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private readonly RegistroEsfuerzoRepository _esfuerzoRepo = new RegistroEsfuerzoRepository();
        private readonly RegistroDefectoRepository _defectoRepo = new RegistroDefectoRepository();
        private readonly InterrupcionRepository _interrupcionRepo = new InterrupcionRepository();
        private readonly FaseRepository _faseRepo = new FaseRepository();

        public ActivityOverviewView()
        {
            InitializeComponent();
        }

        public void CargarMetricasActividad(int actividadId)
        {
            var actividad = _actividadRepo.ObtenerPorId(actividadId);
            if (actividad == null) return;

            TextoRutaProyecto.Text = $"/ {actividad.Proyecto.ToUpper()}";

            // Estado del Proyecto
            if (actividad.Estado == 1)
            {
                BadgeEstadoProyecto.Background = (Brush)new BrushConverter().ConvertFrom("#1C2B20")!;
                TextoIconoEstado.Foreground = (Brush)new BrushConverter().ConvertFrom("#4ADE80")!;
                TextoEstadoProyecto.Foreground = (Brush)new BrushConverter().ConvertFrom("#4ADE80")!;
                TextoIconoEstado.Text = "\uE73E ";
                TextoEstadoProyecto.Text = "COMPLETADO";
            }
            else
            {
                BadgeEstadoProyecto.Background = (Brush)FindResource("BrocheEstadoEnCursoFondo");
                TextoIconoEstado.Foreground = (Brush)FindResource("BrocheEstadoEnCursoTexto");
                TextoEstadoProyecto.Foreground = (Brush)FindResource("BrocheEstadoEnCursoTexto");
                TextoIconoEstado.Text = "\uE916 ";
                TextoEstadoProyecto.Text = "EN CURSO";
            }

            // 1. Tiempos Básicos
            int estMinutos = actividad.TiempoEstimadoMinutos;
            double estHoras = Math.Round(estMinutos / 60.0, 1);

            int realSegundos = _esfuerzoRepo.ObtenerMinutosTotalesPorActividad(actividadId);
            int realMinutos = (int)Math.Round(realSegundos / 60.0);
            double realHoras = Math.Round(realSegundos / 3600.0, 1);

            TextoTiempoEstimadoHoras.Text = estHoras.ToString("0.0");
            TextoTiempoEstimadoMinutos.Text = $"({estMinutos} min)";

            TextoTiempoRealHoras.Text = realHoras.ToString("0.0");
            TextoTiempoRealMinutos.Text = $"({realMinutos} min)";

            double pctConsumo = estMinutos > 0 ? ((double)realMinutos / estMinutos) * 100 : 0;
            BarraConsumoTiempo.Value = Math.Min(pctConsumo, 100);
            TextoPorcentajeConsumo.Text = $"{pctConsumo:0.0}%";

            // 2. Cálculo y Reglas Semánticas de Desviación PSP
            double desviacion = estMinutos > 0 ? ((double)(realMinutos - estMinutos) / estMinutos) * 100 : 0;
            TextoPorcentajeDesviacion.Text = $"{(desviacion >= 0 ? "+" : "")}{desviacion:0.0}%";

            if (realMinutos == 0)
            {
                // CASO ESPECIAL: Sin avance registrado todavía
                TextoPorcentajeDesviacion.Text = "0,0%";
                TextoPorcentajeDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#A1A1AA")!;
                BadgeEstadoDesviacion.Background = (Brush)new BrushConverter().ConvertFrom("#27272A")!;
                IconoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#A1A1AA")!;
                TextoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#A1A1AA")!;
                IconoEstadoDesviacion.Text = "\uE73E ";
                TextoEstadoDesviacion.Text = "Sin tiempo registrado";
            }
            else if (Math.Abs(desviacion) <= 10)
            {
                // ESTIMACIÓN PRECISA (±10%)
                TextoPorcentajeDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#60A5FA")!;
                BadgeEstadoDesviacion.Background = (Brush)new BrushConverter().ConvertFrom("#202B42")!;
                IconoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#60A5FA")!;
                TextoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#60A5FA")!;
                IconoEstadoDesviacion.Text = "\uE73E ";
                TextoEstadoDesviacion.Text = "Estimación precisa (±10%)";
            }
            else if (desviacion > 10 && desviacion <= 50)
            {
                // DESVIACIÓN MODERADA (+10% a +50%)
                TextoPorcentajeDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B")!;
                BadgeEstadoDesviacion.Background = (Brush)new BrushConverter().ConvertFrom("#3B2E1E")!;
                IconoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B")!;
                TextoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#F59E0B")!;
                IconoEstadoDesviacion.Text = "\uE7BA ";
                TextoEstadoDesviacion.Text = "Desviación moderada (+10% a +50%)";
            }
            else if (desviacion > 50)
            {
                // DESVIACIÓN CRÍTICA (> +50%)
                TextoPorcentajeDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF6B6B")!;
                BadgeEstadoDesviacion.Background = (Brush)new BrushConverter().ConvertFrom("#3B2020")!;
                IconoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF6B6B")!;
                TextoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF6B6B")!;
                IconoEstadoDesviacion.Text = "\uE814 ";
                TextoEstadoDesviacion.Text = "Desviación crítica (> +50%)";
            }
            else // desviacion < -10% con realMinutos > 0
            {
                // SOBREESTIMACIÓN (> 10% antes de tiempo)
                TextoPorcentajeDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#10B981")!;
                BadgeEstadoDesviacion.Background = (Brush)new BrushConverter().ConvertFrom("#1C2B20")!;
                IconoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#10B981")!;
                TextoEstadoDesviacion.Foreground = (Brush)new BrushConverter().ConvertFrom("#10B981")!;
                IconoEstadoDesviacion.Text = "\uE73E ";
                TextoEstadoDesviacion.Text = "Completado antes de tiempo";
            }

            // 3. Retrabajo e Interrupciones
            var defectos = _defectoRepo.ObtenerPorActividad(actividadId);
            long segundosRetrabajo = defectos.Sum(d => d.TiempoCorreccionMinutos);
            int minutosRetrabajo = (int)Math.Round(segundosRetrabajo / 60.0);
            double pctRetrabajo = realMinutos > 0 ? ((double)minutosRetrabajo / realMinutos) * 100 : 0;

            TextoPorcentajeRetrabajo.Text = $"{pctRetrabajo:0.0}%";
            TextoTiempoRetrabajo.Text = $"Invertido: {minutosRetrabajo} min";

            int segInterrupcion = _interrupcionRepo.ObtenerSegundosInterrupcionPorActividad(actividadId);
            int minInterrupcion = (int)Math.Round(segInterrupcion / 60.0);
            TextoTotalInterrupciones.Text = $"{minInterrupcion} min";
            TextoTiempoPerdidoInterrupciones.Text = $"Perdido: {minInterrupcion} min";

            // 4. Calidad y Defectos
            int totalDefectos = defectos.Count;
            int corregidos = defectos.Count(d => d.EsResuelto == 1);
            int pendientes = totalDefectos - corregidos;

            TextoDefectosTotales.Text = totalDefectos.ToString("00");
            TextoDefectosCorregidos.Text = corregidos.ToString("00");
            TextoDefectosPendientes.Text = pendientes.ToString("00");

            double tasaResolucion = totalDefectos > 0 ? ((double)corregidos / totalDefectos) * 100 : 100;
            BarraTasaResolucion.Value = tasaResolucion;
            TextoTasaResolucionPct.Text = $"{tasaResolucion:0}%";

            double mttrMinutos = totalDefectos > 0 ? (segundosRetrabajo / 60.0) / totalDefectos : 0;
            TextoPromedioCorreccionMTTR.Text = $"{mttrMinutos:0.0} min";

            // 5. Cargar Distribución por Fase (SIEMPRE EJECUTADO)
            CargarDistribucionFasesDinamica(actividadId, realSegundos);
        }

        private void CargarDistribucionFasesDinamica(int actividadId, int totalSegundosActividad)
        {
            var todasLasFases = _faseRepo.ObtenerTodas();
            var macroFasesDTO = new List<MacroFaseDTO>();

            var coloresMacroFases = new Dictionary<string, string>
            {
                { "Planeación", "#60A5FA" },
                { "Diseño", "#A855F7" },
                { "Construcción", "#9EA8FF" },
                { "Pruebas", "#F59E0B" },
                { "Análisis", "#10B981" }
            };

            var gruposMacroFase = todasLasFases.GroupBy(f => f.MacroFase);

            foreach (var grupo in gruposMacroFase)
            {
                string nombreMacroFase = grupo.Key;
                int segundosMacroFase = 0;
                var listaFasesHijas = new List<FaseDTO>();

                foreach (var fase in grupo)
                {
                    int segFase = _esfuerzoRepo.ObtenerSegundosPorFase(actividadId, fase.Id);
                    segundosMacroFase += segFase;

                    double pctFase = totalSegundosActividad > 0 ? ((double)segFase / totalSegundosActividad) * 100 : 0;

                    listaFasesHijas.Add(new FaseDTO
                    {
                        Id = fase.Id,
                        Nombre = fase.Nombre,
                        MacroFase = fase.MacroFase,
                        Segundos = segFase,
                        Porcentaje = pctFase
                    });
                }

                double pctMacroFase = totalSegundosActividad > 0 ? ((double)segundosMacroFase / totalSegundosActividad) * 100 : 0;
                string colorHex = coloresMacroFases.ContainsKey(nombreMacroFase) ? coloresMacroFases[nombreMacroFase] : "#9EA8FF";

                macroFasesDTO.Add(new MacroFaseDTO
                {
                    Nombre = nombreMacroFase,
                    Segundos = segundosMacroFase,
                    Porcentaje = pctMacroFase,
                    ColorHex = colorHex,
                    FasesHijas = listaFasesHijas
                });
            }

            ListaMacroFases.ItemsSource = macroFasesDTO;
            TextoTotalRegistradoFases.Text = $"{totalSegundosActividad / 60} min";
        }
    }
}