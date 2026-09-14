using System;
using System.Windows;

namespace SistemaMonitoreoProyectos.Models
{
    public class TimeLogItemDTO
    {
        public string FechaFormatted { get; set; } = string.Empty;
        public string Fase { get; set; } = string.Empty;
        public string DuracionFormatted { get; set; } = string.Empty;
        public string InterrupcionesFormatted { get; set; } = "0";
        public string TotalAcumuladoFormatted { get; set; } = string.Empty;
    }

    public class DefectLogItemDTO
    {
        public long Id { get; set; }
        public string FechaRegistroFormatted { get; set; } = string.Empty;
        public string FechaResolucionFormatted { get; set; } = string.Empty;
        public string DescripcionError { get; set; } = string.Empty;
        public string TipoDefectoTexto { get; set; } = string.Empty;
        public string RutaFases { get; set; } = string.Empty;
        public string ImpactoFugaTexto { get; set; } = string.Empty;
        public string ImpactoFugaColor { get; set; } = "#A1A1AA";
        public string TiempoCorreccionFormatted { get; set; } = string.Empty;
        public string EstadoTexto { get; set; } = string.Empty;
        public string EstadoColor { get; set; } = "#A1A1AA";
        public string EsAnidadoVisibility { get; set; } = "Collapsed";
        public Thickness MarginJerarquia { get; set; } = new Thickness(0);
        public string IconoJerarquia { get; set; } = string.Empty;
    }
}