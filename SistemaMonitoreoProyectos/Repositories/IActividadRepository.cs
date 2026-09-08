using SistemaMonitoreoProyectos.Models;
using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IActividadRepository
    {
        int Agregar(Actividad actividad);
        List<Actividad> ObtenerTodas();
        Actividad? ObtenerPorId(int id);
        void ActualizarEstado(int actividadId, int nuevoEstado);
    }
}
