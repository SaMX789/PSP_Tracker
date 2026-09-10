using SistemaMonitoreoProyectos.Models;
using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface ITipoDefectoRepository
    {
        List<TipoDefecto> ObtenerTodos();
    }
}