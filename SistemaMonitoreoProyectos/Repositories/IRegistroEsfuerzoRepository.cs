using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IRegistroEsfuerzoRepository
    {
        long Agregar(RegistroEsfuerzo registro);
        int ObtenerMinutosTotalesPorActividad(int actividadId);
        int ObtenerUltimaFasePorActividad(int actividadId);
        List<int> ObtenerFasesCompletadasPorActividad(int actividadId);
        int ObtenerSegundosPorFase(int actividadId, int faseId);
    }
}