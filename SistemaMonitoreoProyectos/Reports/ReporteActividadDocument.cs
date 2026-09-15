using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaMonitoreoProyectos.Services;
using System;
using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Reports
{
    public class FaseReporteDTO
    {
        public string NombreFase { get; set; } = string.Empty;
        public int MinutosEstimados { get; set; }
        public int MinutosReales { get; set; }
        public int DesviacionMinutos => MinutosReales - MinutosEstimados;
    }

    public class ReporteMetricsDTO
    {
        public int ActividadId { get; set; }
        public string Proyecto { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public bool EsCompletado { get; set; }
        public int TiempoEstimadoMinutos { get; set; }
        public int TiempoRealMinutos { get; set; }
        public int TiempoRetrabajoMinutos { get; set; }
        public int DefectosFugaSeveraCount { get; set; }
        public int TotalDefectos { get; set; }
        public double FactorCalibracion => TiempoEstimadoMinutos > 0 ? (double)TiempoRealMinutos / TiempoEstimadoMinutos : 1.0;

        public List<FaseReporteDTO> FasesDetalle { get; set; } = new List<FaseReporteDTO>();
    }

    public class ReporteActividadDocument : IDocument
    {
        public ReporteMetricsDTO Metricas { get; }

        public ReporteActividadDocument(ReporteMetricsDTO metricas)
        {
            Metricas = metricas;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI").FontColor("#1F2937"));

                page.Header().Element(ComponerEncabezado);
                page.Content().Element(ComponerCuerpo);
                page.Footer().Element(ComponerPiePagina);
            });
        }

        private void ComponerEncabezado(IContainer container)
        {
            string estadoTexto = Metricas.EsCompletado ? "COMPLETADO" : "EN CURSO";
            string estadoColor = Metricas.EsCompletado ? "#10B981" : "#F59E0B";

            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("AS Consultoría Integral").FontSize(16).Bold().FontColor("#1E293B");
                    col.Item().Text("Línea Base y Diagnóstico de Productividad").FontSize(11).FontColor("#64748B");
                });

                row.ConstantItem(140).AlignRight().Column(col =>
                {
                    col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}").FontSize(9).FontColor("#94A3B8");
                    col.Item().Text($"ID Actividad: #{Metricas.ActividadId}").FontSize(9).Bold().FontColor("#3B82F6");
                    col.Item().Text($"Estado: {estadoTexto}").FontSize(9).Bold().FontColor(estadoColor);
                });
            });
        }

        private void ComponerCuerpo(IContainer container)
        {
            double estHoras = Metricas.TiempoEstimadoMinutos / 60.0;
            double realHoras = Metricas.TiempoRealMinutos / 60.0;
            double consumoPct = estHoras > 0 ? (realHoras / estHoras) * 100.0 : 0.0;
            double desviacionPct = estHoras > 0 ? ((realHoras - estHoras) / estHoras) * 100.0 : 0.0;
            double retrabajoPct = realHoras > 0 ? (Metricas.TiempoRetrabajoMinutos / (double)Metricas.TiempoRealMinutos) * 100.0 : 0.0;

            container.PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);

                // Tarjeta Principal
                col.Item().Background("#F8FAFC").Padding(10).Border(1).BorderColor("#E2E8F0").Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Column(sub =>
                        {
                            sub.Item().Text("PROYECTO ANALIZADO").FontSize(8).Bold().FontColor("#64748B");
                            sub.Item().Text(Metricas.Proyecto).FontSize(14).Bold().FontColor("#0F172A");
                        });

                        r.ConstantItem(180).AlignRight().Column(sub =>
                        {
                            sub.Item().Text("RESPONSABLE").FontSize(8).Bold().FontColor("#64748B");
                            sub.Item().Text(Metricas.Responsable).FontSize(11).Bold().FontColor("#3B82F6");
                        });
                    });
                });

                // KPIs Superiores
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => TarjetaKpi(e, "ESTIMADO GLOBAL", $"{estHoras:0.0} hrs", "#3B82F6"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "REAL REGISTRADO", $"{realHoras:0.0} hrs ({Metricas.TiempoRealMinutos} min)", "#10B981"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "CONSUMO PLAN", $"{consumoPct:0.0}%", consumoPct > 100 ? "#EF4444" : "#3B82F6"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "TASA DE RETRABAJO", $"{retrabajoPct:0.0}%", retrabajoPct > 15 ? "#EF4444" : "#F59E0B"));
                });

                // Diagnóstico Narrativo
                col.Item().Background("#EFF6FF").Border(1).BorderColor("#BFDBFE").Padding(12).Column(c =>
                {
                    c.Item().Text("EVALUACIÓN EJECUTIVA").FontSize(9).Bold().FontColor("#1D4ED8");
                    c.Item().PaddingTop(4).Text(ReportNarrativeService.ObtenerDiagnosticoEstimacion(desviacionPct, Metricas.FactorCalibracion, Metricas.EsCompletado, consumoPct)).FontSize(9.5f);
                    c.Item().PaddingTop(2).Text(ReportNarrativeService.ObtenerDiagnosticoCalidad(retrabajoPct, Metricas.DefectosFugaSeveraCount)).FontSize(9.5f);
                });

                // --- NUEVAS SECCIONES DE VALOR ESTRATÉGICO ---
                ComponerTablaFases(col);
                ComponerAnalisisCalidad(col);

                // Dictamen Final
                col.Item().PaddingTop(10).Background("#F8FAFC").BorderLeft(4).BorderColor(Metricas.EsCompletado ? "#10B981" : "#3B82F6").Padding(10).Column(c =>
                {
                    c.Item().Text(ReportNarrativeService.ObtenerDictamenFinal(Metricas.FactorCalibracion, retrabajoPct, Metricas.EsCompletado)).Bold().FontSize(10).FontColor("#1E293B");
                });
            });
        }

        private void ComponerTablaFases(ColumnDescriptor col)
        {
            col.Item().PaddingTop(10).Text("CONSTRUCCIÓN DE LÍNEA BASE (DESGLOSE POR FASE)").FontSize(10).Bold().FontColor("#1D4ED8");

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Fase
                    columns.RelativeColumn(1); // Estimado
                    columns.RelativeColumn(1); // Real
                    columns.RelativeColumn(1); // Desviación
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderColor("#94A3B8").PaddingBottom(4).Text("FASE DE DESARROLLO").FontSize(8).Bold().FontColor("#64748B");
                    header.Cell().BorderBottom(1).BorderColor("#94A3B8").PaddingBottom(4).AlignRight().Text("PLAN (MIN)").FontSize(8).Bold().FontColor("#64748B");
                    header.Cell().BorderBottom(1).BorderColor("#94A3B8").PaddingBottom(4).AlignRight().Text("REAL (MIN)").FontSize(8).Bold().FontColor("#64748B");
                    header.Cell().BorderBottom(1).BorderColor("#94A3B8").PaddingBottom(4).AlignRight().Text("DESVIACIÓN").FontSize(8).Bold().FontColor("#64748B");
                });

                foreach (var f in Metricas.FasesDetalle)
                {
                    string colorDesv = f.DesviacionMinutos > 0 ? "#EF4444" : "#10B981";
                    string signo = f.DesviacionMinutos > 0 ? "+" : "";

                    table.Cell().BorderBottom(1).BorderColor("#F1F5F9").PaddingVertical(4).Text(f.NombreFase).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor("#F1F5F9").PaddingVertical(4).AlignRight().Text($"{f.MinutosEstimados}").FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor("#F1F5F9").PaddingVertical(4).AlignRight().Text($"{f.MinutosReales}").FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor("#F1F5F9").PaddingVertical(4).AlignRight().Text($"{signo}{f.DesviacionMinutos} min").FontSize(9).Bold().FontColor(colorDesv);
                }
            });
        }

        private void ComponerAnalisisCalidad(ColumnDescriptor col)
        {
            col.Item().PaddingTop(10).Text("ANÁLISIS DE COSTO DE LA NO CALIDAD Y PRONÓSTICO").FontSize(10).Bold().FontColor("#1D4ED8");

            col.Item().PaddingTop(4).Row(row =>
            {
                // Tarjeta de Fugas de Calidad
                row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(10).Column(c =>
                {
                    c.Item().Text("COSTO DE RETRABAJO (DEFECTOS)").FontSize(8).Bold().FontColor("#64748B");
                    c.Item().PaddingTop(4).Text($"{Metricas.TotalDefectos} Errores encontrados en total.").FontSize(10).Bold().FontColor("#0F172A");

                    if (Metricas.DefectosFugaSeveraCount > 0)
                        c.Item().Text($"⚠️ {Metricas.DefectosFugaSeveraCount} Fugas Severas detectadas tarde.").FontSize(9).FontColor("#EF4444").Bold();
                    else
                        c.Item().Text($"✓ Cero fugas severas detectadas.").FontSize(9).FontColor("#10B981").Bold();

                    c.Item().PaddingTop(4).Text($"Impacto al proyecto: {Metricas.TiempoRetrabajoMinutos} min perdidos corrigiendo.").FontSize(9).FontColor("#F59E0B").Bold();
                });

                row.ConstantItem(10);

                // Tarjeta de Factor de Calibración
                row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(10).Column(c =>
                {
                    c.Item().Text("FACTOR DE CALIBRACIÓN (FUTURO)").FontSize(8).Bold().FontColor("#64748B");
                    c.Item().PaddingTop(2).Text($"{Metricas.FactorCalibracion:0.00}x").FontSize(16).Bold().FontColor("#3B82F6");

                    string recomendacion = Metricas.FactorCalibracion > 1.10
                        ? $"Para futuros proyectos similares, multiplique la estimación original de los ingenieros por {Metricas.FactorCalibracion:0.00} para asegurar un compromiso de entrega realista."
                        : "El equipo estima con altísima precisión. No es necesario aplicar factores de holgura agresivos.";

                    c.Item().PaddingTop(4).Text(recomendacion).FontSize(8).FontColor("#475569").Italic();
                });
            });
        }

        private void TarjetaKpi(IContainer container, string titulo, string valor, string colorHex)
        {
            container.Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
            {
                c.Item().Text(titulo).FontSize(7).Bold().FontColor("#64748B");
                c.Item().Text(valor).FontSize(12).Bold().FontColor(colorHex);
            });
        }

        private void ComponerPiePagina(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("AS Consultoría Integral — Información de Línea Base Histórica").FontSize(8).FontColor("#94A3B8");
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Página ").FontSize(8).FontColor("#94A3B8");
                    x.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                });
            });
        }
    }
}