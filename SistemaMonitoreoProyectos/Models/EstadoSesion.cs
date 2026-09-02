using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class EstadoSesion
    {
        public int Id { get; set; } = 1;
        public int? ActividadId { get; set; }
        public string? FaseActual { get; set; }
        public int EstadoCronometro { get; set; } = 0; // 0: Pausado, 1: Corriendo
        public DateTime? FechaInicioSesion { get; set; }
        public int MinutosAcumulados { get; set; } = 0;
        public DateTime? UltimaActualizacion { get; set; }
    }
}
