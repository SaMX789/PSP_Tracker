using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class RegistroEsfuerzo
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public string Fase { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; } = DateTime.Now;
        public DateTime? FechaFin { get; set; }
        public int MinutosEfectivos { get; set; }
    }
}
