using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Models
{
    public class FaseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string MacroFase { get; set; } = string.Empty;
        public int Segundos { get; set; }
        public double Porcentaje { get; set; }
        public string MinutosTexto => $"{Segundos / 60} min";
        public string PorcentajeTexto => $"{Porcentaje:0.0}%";
    }

    public class MacroFaseDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public int Segundos { get; set; }
        public double Porcentaje { get; set; }
        public string ColorHex { get; set; } = "#9EA8FF";
        public string MinutosTexto => $"{Segundos / 60} min";
        public string PorcentajeTexto => $"{Porcentaje:0.0}%";
        public List<FaseDTO> FasesHijas { get; set; } = new List<FaseDTO>();
    }
}