using System.Collections.Generic;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IFaseRepository
    {
        List<Fase> ObtenerTodas();
        Fase? ObtenerPorId(int id);
        List<Fase> ObtenerPorMacroFase(string macroFase);
    }
}