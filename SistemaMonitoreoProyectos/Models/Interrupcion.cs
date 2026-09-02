using System;

namespace SistemaMonitoreoProyectos.Models
{
    public class Interrupcion
    {
        public int Id { get; set; }
        public int RegistroEsfuerzoId { get; set; }
        public int DuracionMinutos { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}
