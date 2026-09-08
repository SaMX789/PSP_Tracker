using System;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class EstadoSesionRepository : IEstadoSesionRepository
    {
        public void GuardarOSustituirSesion(EstadoSesion nuevaSesion)
        {
            // 1. REVISAR LA SESIÓN PREVIA EN BASE DE DATOS
            var sesionActual = ObtenerSesion();

            // 2. RED DE SEGURIDAD (Auto-Commit):
            // Si la sesión anterior pertenecía a OTRA actividad y tenía minutos acumulados sin guardar,
            // respaldamos ese tiempo en RegistrosEsfuerzo antes de sobreescribir.
            if (sesionActual.ActividadId.HasValue &&
                sesionActual.ActividadId != nuevaSesion.ActividadId &&
                sesionActual.MinutosAcumulados > 0)
            {
                var registroRepo = new RegistroEsfuerzoRepository();
                registroRepo.Agregar(new RegistroEsfuerzo
                {
                    ActividadId = sesionActual.ActividadId.Value,
                    FaseId = sesionActual.FaseActualId ?? 1,
                    MinutosEfectivos = sesionActual.MinutosAcumulados,
                    FechaInicio = sesionActual.FechaInicioSesion ?? DateTime.Now.AddMinutes(-sesionActual.MinutosAcumulados),
                    FechaFin = DateTime.Now
                });
            }

            // 3. SOBREESCRIBIR LA FILA ÚNICA EN EstadoSesion
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                INSERT INTO EstadoSesion (Id, ActividadId, FaseActualId, EstadoCronometro, FechaInicioSesion, MinutosAcumulados, UltimaActualizacion)
                VALUES (1, @actividadId, @faseId, @estadoCronometro, @fechaInicio, @minutosAcumulados, @ultimaActualizacion)
                ON CONFLICT(Id) DO UPDATE SET
                    ActividadId = excluded.ActividadId,
                    FaseActualId = excluded.FaseActualId,
                    EstadoCronometro = excluded.EstadoCronometro,
                    FechaInicioSesion = excluded.FechaInicioSesion,
                    MinutosAcumulados = excluded.MinutosAcumulados,
                    UltimaActualizacion = excluded.UltimaActualizacion;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", (object?)nuevaSesion.ActividadId ?? DBNull.Value);
            comando.Parameters.AddWithValue("@faseId", (object?)nuevaSesion.FaseActualId ?? DBNull.Value);
            comando.Parameters.AddWithValue("@estadoCronometro", nuevaSesion.EstadoCronometro);

            // Conversión segura para FechaInicioSesion
            comando.Parameters.AddWithValue("@fechaInicio", nuevaSesion.FechaInicioSesion != null
                ? Convert.ToDateTime(nuevaSesion.FechaInicioSesion).ToString("yyyy-MM-dd HH:mm:ss")
                : DBNull.Value);

            comando.Parameters.AddWithValue("@minutosAcumulados", nuevaSesion.MinutosAcumulados);

            // Conversión segura para UltimaActualizacion (Solución al error CS1501 en línea 57)
            comando.Parameters.AddWithValue("@ultimaActualizacion", Convert.ToDateTime(nuevaSesion.UltimaActualizacion).ToString("yyyy-MM-dd HH:mm:ss"));

            comando.ExecuteNonQuery();
        }

        public EstadoSesion ObtenerSesion()
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT ActividadId, FaseActualId, EstadoCronometro, FechaInicioSesion, MinutosAcumulados, UltimaActualizacion FROM EstadoSesion WHERE Id = 1;";

            using var comando = new SqliteCommand(sql, conexion);
            using var reader = comando.ExecuteReader();

            if (reader.Read())
            {
                return new EstadoSesion
                {
                    ActividadId = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    FaseActualId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    EstadoCronometro = reader.GetInt32(2),
                    FechaInicioSesion = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                    MinutosAcumulados = reader.GetInt32(4),
                    UltimaActualizacion = DateTime.Parse(reader.GetString(5))
                };
            }

            return new EstadoSesion();
        }
    }
}