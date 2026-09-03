using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class RegistroDefecto
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public int? DefectoPadreId { get; set; }
        public string DescripcionError { get; set; } = string.Empty;
        public int FaseOrigenId { get; set; }    
        public int FaseDeteccionId { get; set; } 
        public int TiempoCorreccionMinutos { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
