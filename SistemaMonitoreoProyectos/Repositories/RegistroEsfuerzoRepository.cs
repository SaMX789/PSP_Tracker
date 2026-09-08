using System;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class RegistroEsfuerzoRepository : IRegistroEsfuerzoRepository
    {
        public long Agregar(RegistroEsfuerzo registro)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                INSERT INTO RegistrosEsfuerzo (ActividadId, FaseId, MinutosEfectivos, FechaInicio, FechaFin)
                VALUES (@actividadId, @faseId, @minutosEfectivos, @fechaInicio, @fechaFin);
                SELECT last_insert_rowid();";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", registro.ActividadId);
            comando.Parameters.AddWithValue("@faseId", registro.FaseId);
            comando.Parameters.AddWithValue("@minutosEfectivos", registro.MinutosEfectivos);
            comando.Parameters.AddWithValue("@fechaInicio", Convert.ToDateTime(registro.FechaInicio).ToString("yyyy-MM-dd HH:mm:ss"));
            comando.Parameters.AddWithValue("@fechaFin", Convert.ToDateTime(registro.FechaFin).ToString("yyyy-MM-dd HH:mm:ss"));

            object resultado = comando.ExecuteScalar() ?? 0;
            return Convert.ToInt64(resultado);
        }

        public int ObtenerMinutosTotalesPorActividad(int actividadId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT COALESCE(SUM(MinutosEfectivos), 0) FROM RegistrosEsfuerzo WHERE ActividadId = @actividadId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            object resultado = comando.ExecuteScalar() ?? 0;
            return Convert.ToInt32(resultado);
        }
    }
}