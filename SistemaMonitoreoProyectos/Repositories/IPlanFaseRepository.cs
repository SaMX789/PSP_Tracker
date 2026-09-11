using System.Collections.Generic;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IPlanFaseRepository
    {
        void GuardarOActualizarLista(List<PlanFase> planes);
        List<PlanFase> ObtenerPorActividad(int actividadId);
        PlanFase? ObtenerPorActividadYFase(int actividadId, int faseId);
    }
}