using SistemaMonitoreoProyectos.Models;
using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IInterrupcionRepository
    {
        void Agregar(Interrupcion interrupcion);
        int ObtenerSegundosInterrupcionPorActividad(int actividadId);
        Dictionary<int, int> ObtenerConteoInterrupcionesPorActividad(int actividadId);
    }
}