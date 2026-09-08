using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IInterrupcionRepository
    {
        void Agregar(Interrupcion interrupcion);
        int ObtenerSegundosInterrupcionPorActividad(int actividadId);
    }
}
