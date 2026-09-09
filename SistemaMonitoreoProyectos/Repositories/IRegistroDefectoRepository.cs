using System.Collections.Generic;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IRegistroDefectoRepository
    {
        long Agregar(RegistroDefecto defecto);
        void Actualizar(RegistroDefecto defecto);
        void Eliminar(long id);
        List<RegistroDefecto> ObtenerPorActividad(long actividadId);
        int ObtenerConteoDefectosPorActividad(long actividadId);
    }
}