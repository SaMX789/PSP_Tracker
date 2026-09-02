using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class Actividad
    {
        public int Id { get; set; }
        public string Proyecto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public int TiempoEstimadoMinutos { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }
        public int Estado { get; set; } = 0; // 0: En curso, 1: Terminado
    }
}
