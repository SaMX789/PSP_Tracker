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
            AsegurarColumnaEsResuelto();
        }

        private void AsegurarColumnaEsResuelto()
        {
            try
            {
                using var conexion = ConexionDB.ObtenerConexion();
                var sql = "ALTER TABLE RegistrosDefectos ADD COLUMN EsResuelto INTEGER DEFAULT 0;";
                using var comando = new SqliteCommand(sql, conexion);
                comando.ExecuteNonQuery();
            }
            catch
            {
                // La columna ya existe en SQLite
            }
        }

        public long Agregar(RegistroDefecto defecto)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                INSERT INTO RegistrosDefectos 
                (ActividadId, DefectoPadreId, DescripcionError, FaseOrigenId, FaseDeteccionId, TiempoCorreccionMinutos, FechaRegistro, EsResuelto)
                VALUES 
                (@actividadId, @defectoPadreId, @descripcionError, @faseOrigenId, @faseDeteccionId, @tiempoCorreccionMinutos, @fechaRegistro, @esResuelto);
                SELECT last_insert_rowid();";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", defecto.ActividadId);
            comando.Parameters.AddWithValue("@defectoPadreId", (object?)defecto.DefectoPadreId ?? DBNull.Value);
            comando.Parameters.AddWithValue("@descripcionError", defecto.DescripcionError);
            comando.Parameters.AddWithValue("@faseOrigenId", defecto.FaseOrigenId);
            comando.Parameters.AddWithValue("@faseDeteccionId", defecto.FaseDeteccionId);
            comando.Parameters.AddWithValue("@tiempoCorreccionMinutos", defecto.TiempoCorreccionMinutos);
            comando.Parameters.AddWithValue("@fechaRegistro", defecto.FechaRegistro);
            comando.Parameters.AddWithValue("@esResuelto", defecto.EsResuelto);

            object resultado = comando.ExecuteScalar() ?? 0;
            return Convert.ToInt64(resultado);
        }

        public void Actualizar(RegistroDefecto defecto)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
        UPDATE RegistrosDefectos 
        SET DescripcionError = @descripcionError,
            FaseOrigenId = @faseOrigenId,
            FaseDeteccionId = @faseDeteccionId,
            TiempoCorreccionMinutos = @tiempoCorreccionSegundos,
            EsResuelto = @esResuelto
        WHERE Id = @id;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", defecto.Id);
            comando.Parameters.AddWithValue("@descripcionError", defecto.DescripcionError);
            comando.Parameters.AddWithValue("@faseOrigenId", defecto.FaseOrigenId);
            comando.Parameters.AddWithValue("@faseDeteccionId", defecto.FaseDeteccionId);
            comando.Parameters.AddWithValue("@tiempoCorreccionSegundos", defecto.TiempoCorreccionMinutos); // Guarda el total exacto en segundos
            comando.Parameters.AddWithValue("@esResuelto", defecto.EsResuelto);

            comando.ExecuteNonQuery();
        }

        public void Eliminar(long id)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            // Elimina el defecto e hilos anidados directos
            var sql = "DELETE FROM RegistrosDefectos WHERE Id = @id OR DefectoPadreId = @id;";
            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.ExecuteNonQuery();
        }

        public List<RegistroDefecto> ObtenerPorActividad(long actividadId)
        {
            var lista = new List<RegistroDefecto>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"SELECT Id, ActividadId, DefectoPadreId, DescripcionError, FaseOrigenId, FaseDeteccionId, TiempoCorreccionMinutos, FechaRegistro, COALESCE(EsResuelto, 0)
                        FROM RegistrosDefectos 
                        WHERE ActividadId = @actividadId 
                        ORDER BY Id ASC;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

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
                    EsResuelto = reader.GetInt32(8)
                });
            }
            return lista;
        }

        public int ObtenerConteoDefectosPorActividad(long actividadId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT COUNT(*) FROM RegistrosDefectos WHERE ActividadId = @actividadId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            return Convert.ToInt32(comando.ExecuteScalar() ?? 0);
        }
    }
}