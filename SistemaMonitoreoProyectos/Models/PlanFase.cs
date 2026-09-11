namespace SistemaMonitoreoProyectos.Models
{
    public class PlanFase
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public int FaseId { get; set; }
        public int TiempoEstimadoMinutos { get; set; } = 0;
        public int DefectosEstimadosInyectados { get; set; } = 0;
        public int DefectosEstimadosRemovidos { get; set; } = 0;
    }
}