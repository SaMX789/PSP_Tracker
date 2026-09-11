using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class RegistroDefectoRepository : IRegistroDefectoRepository
    {
        public RegistroDefectoRepository()
        {
            
        }

        public long Agregar(RegistroDefecto defecto)
        {
            using var conexion = ConexionDB.ObtenerConexion();

            string query = @"
                INSERT INTO RegistrosDefectos 
                (ActividadId, DefectoPadreId, DescripcionError, FaseOrigenId, FaseDeteccionId, 
                 TiempoCorreccionMinutos, FechaRegistro, EsResuelto, TipoDefectoId, FechaResolucion)
                VALUES 
                (@ActividadId, @DefectoPadreId, @DescripcionError, @FaseOrigenId, @FaseDeteccionId, 
                 @TiempoCorreccionMinutos, @FechaRegistro, @EsResuelto, @TipoDefectoId, @FechaResolucion);
                SELECT last_insert_rowid();";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@ActividadId", defecto.ActividadId);
            comando.Parameters.AddWithValue("@DefectoPadreId", defecto.DefectoPadreId ?? (object)DBNull.Value);
            comando.Parameters.AddWithValue("@DescripcionError", defecto.DescripcionError);
            comando.Parameters.AddWithValue("@FaseOrigenId", defecto.FaseOrigenId);
            comando.Parameters.AddWithValue("@FaseDeteccionId", defecto.FaseDeteccionId);
            comando.Parameters.AddWithValue("@TiempoCorreccionMinutos", defecto.TiempoCorreccionMinutos);
            comando.Parameters.AddWithValue("@FechaRegistro", defecto.FechaRegistro);
            comando.Parameters.AddWithValue("@EsResuelto", defecto.EsResuelto);
            comando.Parameters.AddWithValue("@TipoDefectoId", defecto.TipoDefectoId ?? (object)DBNull.Value);
            comando.Parameters.AddWithValue("@FechaResolucion", defecto.FechaResolucion ?? (object)DBNull.Value);

            var resultado = comando.ExecuteScalar();
            return resultado != null && resultado != DBNull.Value ? Convert.ToInt64(resultado) : 0L;
        }

        public void Actualizar(RegistroDefecto defecto)
        {
            using var conexion = ConexionDB.ObtenerConexion();

            string query = @"
                UPDATE RegistrosDefectos 
                SET DescripcionError = @DescripcionError,
                    FaseOrigenId = @FaseOrigenId,
                    FaseDeteccionId = @FaseDeteccionId,
                    TiempoCorreccionMinutos = @TiempoCorreccionMinutos,
                    EsResuelto = @EsResuelto,
                    TipoDefectoId = @TipoDefectoId,
                    FechaResolucion = @FechaResolucion
                WHERE Id = @Id;";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@Id", defecto.Id);
            comando.Parameters.AddWithValue("@DescripcionError", defecto.DescripcionError);
            comando.Parameters.AddWithValue("@FaseOrigenId", defecto.FaseOrigenId);
            comando.Parameters.AddWithValue("@FaseDeteccionId", defecto.FaseDeteccionId);
            comando.Parameters.AddWithValue("@TiempoCorreccionMinutos", defecto.TiempoCorreccionMinutos);
            comando.Parameters.AddWithValue("@EsResuelto", defecto.EsResuelto);
            comando.Parameters.AddWithValue("@TipoDefectoId", defecto.TipoDefectoId ?? (object)DBNull.Value);
            comando.Parameters.AddWithValue("@FechaResolucion", defecto.FechaResolucion ?? (object)DBNull.Value);

            comando.ExecuteNonQuery();
        }

        public void Eliminar(long id)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            string query = "DELETE FROM RegistrosDefectos WHERE Id = @Id;";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@Id", id);

            comando.ExecuteNonQuery();
        }

        public List<RegistroDefecto> ObtenerPorActividad(long actividadId)
        {
            var lista = new List<RegistroDefecto>();
            using var conexion = ConexionDB.ObtenerConexion();

            string query = @"
                SELECT Id, ActividadId, DefectoPadreId, DescripcionError, FaseOrigenId, 
                       FaseDeteccionId, TiempoCorreccionMinutos, FechaRegistro, EsResuelto, 
                       TipoDefectoId, FechaResolucion
                FROM RegistrosDefectos
                WHERE ActividadId = @ActividadId
                ORDER BY Id ASC;";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@ActividadId", actividadId);
            using var reader = comando.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new RegistroDefecto
                {
                    Id = reader.GetInt64(0),
                    ActividadId = reader.GetInt64(1),
                    DefectoPadreId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    DescripcionError = reader.GetString(3),
                    FaseOrigenId = reader.GetInt64(4),
                    FaseDeteccionId = reader.GetInt64(5),
                    TiempoCorreccionMinutos = reader.GetInt64(6),
                    FechaRegistro = reader.GetString(7),
                    EsResuelto = reader.GetInt32(8),
                    TipoDefectoId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    FechaResolucion = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }

            return lista;
        }

        public int ObtenerConteoDefectosPorActividad(long actividadId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            string query = "SELECT COUNT(Id) FROM RegistrosDefectos WHERE ActividadId = @ActividadId;";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@ActividadId", actividadId);

            return Convert.ToInt32(comando.ExecuteScalar());
        }
    }
}