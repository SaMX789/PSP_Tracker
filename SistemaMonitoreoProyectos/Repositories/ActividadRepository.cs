using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class ActividadRepository: IActividadRepository
    {
        public int Agregar(Actividad actividad)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                INSERT INTO Actividades (Proyecto, Descripcion, Responsable, TiempoEstimadoMinutos, FechaInicio, Estado)
                VALUES (@proyecto, @descripcion, @responsable, @tiempoEstimado, @fechaInicio, @estado);
                SELECT last_insert_rowid();";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@proyecto", actividad.Proyecto);
            comando.Parameters.AddWithValue("@descripcion", actividad.Descripcion);
            comando.Parameters.AddWithValue("@responsable", actividad.Responsable);
            comando.Parameters.AddWithValue("@tiempoEstimado", actividad.TiempoEstimadoMinutos);
            comando.Parameters.AddWithValue("@fechaInicio", actividad.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss"));
            comando.Parameters.AddWithValue("@estado", actividad.Estado);

            // Retorna el Id autogenerado por SQLite
            long idGenerado = (long)comando.ExecuteScalar()!;
            return (int)idGenerado;
        }

        public List<Actividad> ObtenerTodas()
        {
            var lista = new List<Actividad>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT Id, Proyecto, Descripcion, Responsable, TiempoEstimadoMinutos, FechaInicio, FechaCierre, Estado FROM Actividades ORDER BY Id DESC;";

            using var comando = new SqliteCommand(sql, conexion);
            using var lector = comando.ExecuteReader();

            while (lector.Read())
            {
                lista.Add(new Actividad
                {
                    Id = lector.GetInt32(0),
                    Proyecto = lector.GetString(1),
                    Descripcion = lector.GetString(2),
                    Responsable = lector.GetString(3),
                    TiempoEstimadoMinutos = lector.GetInt32(4),
                    FechaInicio = DateTime.Parse(lector.GetString(5)),
                    FechaCierre = lector.IsDBNull(6) ? null : DateTime.Parse(lector.GetString(6)),
                    Estado = lector.GetInt32(7)
                });
            }

            return lista;
        }

        public Actividad? ObtenerPorId(int id)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT Id, Proyecto, Descripcion, Responsable, TiempoEstimadoMinutos, FechaInicio, FechaCierre, Estado FROM Actividades WHERE Id = @id;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            using var lector = comando.ExecuteReader();

            if (lector.Read())
            {
                return new Actividad
                {
                    Id = lector.GetInt32(0),
                    Proyecto = lector.GetString(1),
                    Descripcion = lector.GetString(2),
                    Responsable = lector.GetString(3),
                    TiempoEstimadoMinutos = lector.GetInt32(4),
                    FechaInicio = DateTime.Parse(lector.GetString(5)),
                    FechaCierre = lector.IsDBNull(6) ? null : DateTime.Parse(lector.GetString(6)),
                    Estado = lector.GetInt32(7)
                };
            }

            return null;
        }

        public void ActualizarEstado(int actividadId, int nuevoEstado)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "UPDATE Actividades SET Estado = @estado, FechaCierre = @fechaCierre WHERE Id = @id;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@estado", nuevoEstado);
            comando.Parameters.AddWithValue("@fechaCierre", nuevoEstado == 1 ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
            comando.Parameters.AddWithValue("@id", actividadId);

            comando.ExecuteNonQuery();
        }
    }
}
