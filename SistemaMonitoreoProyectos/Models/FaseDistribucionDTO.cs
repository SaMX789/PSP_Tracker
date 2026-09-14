using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Models
{
    public class FaseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string MacroFase { get; set; } = string.Empty;

        // Métricas Reales
        public int Segundos { get; set; }
        public double Porcentaje { get; set; }
        public string MinutosTexto => FormatearTiempo(Segundos / 60);
        public string PorcentajeTexto => $"{Porcentaje:0.0}%";

        // Métricas Estimadas (Plan)
        public int MinutosEstimados { get; set; }
        public double PorcentajeEstimado { get; set; }
        public string MinutosEstimadosTexto => FormatearTiempo(MinutosEstimados);
        public string PorcentajeEstimadoTexto => $"{PorcentajeEstimado:0.0}%";

        public static string FormatearTiempo(int totalMinutos)
        {
            if (totalMinutos <= 0) return "0 min";
            int hrs = totalMinutos / 60;
            int mins = totalMinutos % 60;

            if (hrs == 0) return $"{mins} min";
            if (mins == 0) return $"{hrs} h";
            return $"{hrs} h {mins} min";
        }
    }

    public class MacroFaseDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#9EA8FF";

        // Métricas Reales
        public int Segundos { get; set; }
        public double Porcentaje { get; set; }
        public string MinutosTexto => FaseDTO.FormatearTiempo(Segundos / 60);
        public string PorcentajeTexto => $"{Porcentaje:0.0}%";

        // Métricas Estimadas (Plan)
        public int MinutosEstimados { get; set; }
        public double PorcentajeEstimado { get; set; }
        public string MinutosEstimadosTexto => FaseDTO.FormatearTiempo(MinutosEstimados);
        public string PorcentajeEstimadoTexto => $"{PorcentajeEstimado:0.0}%";

        public List<FaseDTO> FasesHijas { get; set; } = new List<FaseDTO>();
    }
}