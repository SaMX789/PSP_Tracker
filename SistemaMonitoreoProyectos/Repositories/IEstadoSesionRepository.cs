using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public interface IEstadoSesionRepository
    {
        void GuardarOSustituirSesion(EstadoSesion nuevaSesion);
        EstadoSesion ObtenerSesion();
    }
}