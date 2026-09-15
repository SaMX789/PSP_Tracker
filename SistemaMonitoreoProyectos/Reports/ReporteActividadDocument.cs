using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaMonitoreoProyectos.Services;
using System;

namespace SistemaMonitoreoProyectos.Reports
{
    public class ReporteMetricsDTO
    {
        public int ActividadId { get; set; }
        public string Proyecto { get; set; } = string.Empty;
        public bool EsCompletado { get; set; }
        public int TiempoEstimadoMinutos { get; set; }
        public int TiempoRealMinutos { get; set; }
        public int TiempoRetrabajoMinutos { get; set; }
        public int DefectosFugaSeveraCount { get; set; }
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
            string estadoColor = Metricas.EsCompletado ? "#10B981" : "#3B82F6";

            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("AS Consultoría Integral").FontSize(16).Bold().FontColor("#1E293B");
                    col.Item().Text("PSP Tracker — Reporte de Rendimiento y Calidad").FontSize(11).FontColor("#64748B");
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
            double factorCalib = estHoras > 0 ? realHoras / estHoras : 1.0;
            double retrabajoPct = realHoras > 0 ? (Metricas.TiempoRetrabajoMinutos / (double)Metricas.TiempoRealMinutos) * 100.0 : 0.0;

            container.PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);

                col.Item().Background("#F8FAFC").Padding(10).Border(1).BorderColor("#E2E8F0").Column(c =>
                {
                    c.Item().Text("PROYECTO ANALIZADO").FontSize(8).Bold().FontColor("#64748B");
                    c.Item().Text(Metricas.Proyecto).FontSize(14).Bold().FontColor("#0F172A");
                });

                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => TarjetaKpi(e, "ESTIMADO", $"{estHoras:0.0} hrs", "#3B82F6"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "REAL REGISTRADO", $"{realHoras:0.0} hrs ({Metricas.TiempoRealMinutos} min)", "#10B981"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "CONSUMO PLAN", $"{consumoPct:0.0}%", consumoPct > 100 ? "#EF4444" : "#3B82F6"));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(e => TarjetaKpi(e, "RETRABAJO", $"{retrabajoPct:0.0}%", retrabajoPct > 15 ? "#EF4444" : "#F59E0B"));
                });

                col.Item().Background("#EFF6FF").Border(1).BorderColor("#BFDBFE").Padding(12).Column(c =>
                {
                    c.Item().Text("EVALUACIÓN Y DIAGNÓSTICO EJECUTIVO").FontSize(9).Bold().FontColor("#1D4ED8");
                    c.Item().PaddingTop(4).Text(ReportNarrativeService.ObtenerDiagnosticoEstimacion(desviacionPct, factorCalib, Metricas.EsCompletado, consumoPct)).FontSize(9.5f);
                    c.Item().PaddingTop(2).Text(ReportNarrativeService.ObtenerDiagnosticoCalidad(retrabajoPct, Metricas.DefectosFugaSeveraCount)).FontSize(9.5f);
                });

                col.Item().Background("#F8FAFC").BorderLeft(4).BorderColor(Metricas.EsCompletado ? "#10B981" : "#3B82F6").Padding(10).Column(c =>
                {
                    c.Item().Text(ReportNarrativeService.ObtenerDictamenFinal(factorCalib, retrabajoPct, Metricas.EsCompletado)).Bold().FontSize(10).FontColor("#1E293B");
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
                row.RelativeItem().Text("AS Consultoría Integral — Uso Interno Confidencial").FontSize(8).FontColor("#94A3B8");
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Página ").FontSize(8).FontColor("#94A3B8");
                    x.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                });
            });
        }
    }
}