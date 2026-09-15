using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Models
{
    public class ActividadBackupDTO
    {
        public Actividad Actividad { get; set; } = new Actividad();
        public List<PlanFase> Planes { get; set; } = new List<PlanFase>();
        public List<RegistroEsfuerzo> Esfuerzos { get; set; } = new List<RegistroEsfuerzo>();
        public List<RegistroDefecto> Defectos { get; set; } = new List<RegistroDefecto>();
        public List<Interrupcion> Interrupciones { get; set; } = new List<Interrupcion>();
    }
}