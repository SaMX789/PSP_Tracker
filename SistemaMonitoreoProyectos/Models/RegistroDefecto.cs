using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class RegistroDefecto
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public int? DefectoPadreId { get; set; } // Nullable para permitir anidación
        public string DescripcionError { get; set; } = string.Empty;
        public string FaseOrigen { get; set; } = string.Empty;
        public string FaseDeteccion { get; set; } = string.Empty;
        public int TiempoCorreccionMinutos { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
