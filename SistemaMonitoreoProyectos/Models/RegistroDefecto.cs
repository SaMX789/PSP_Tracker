using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class RegistroDefecto
    {
        public long Id { get; set; }
        public long ActividadId { get; set; }
        public long? DefectoPadreId { get; set; }
        public string DescripcionError { get; set; } = string.Empty;
        public long FaseOrigenId { get; set; }
        public long FaseDeteccionId { get; set; }
        public long TiempoCorreccionMinutos { get; set; }
        public string FechaRegistro { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public int EsResuelto { get; set; } = 0; // 0: Pendiente (Rojo), 1: Resuelto (Verde)
    }
}