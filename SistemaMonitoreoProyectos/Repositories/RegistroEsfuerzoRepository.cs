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
        public int ObtenerUltimaFasePorActividad(int actividadId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT FaseId FROM RegistrosEsfuerzo WHERE ActividadId = @actividadId ORDER BY Id DESC LIMIT 1;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            object? resultado = comando.ExecuteScalar();
            if (resultado != null && resultado != DBNull.Value)
            {
                return Convert.ToInt32(resultado);
            }

            return 1; // Planning por defecto si no tiene registros previos
        }
        public List<int> ObtenerFasesCompletadasPorActividad(int actividadId)
        {
            var fasesCompletadas = new List<int>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT DISTINCT FaseId FROM RegistrosEsfuerzo WHERE ActividadId = @actividadId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            using var reader = comando.ExecuteReader();
            while (reader.Read())
            {
                fasesCompletadas.Add(reader.GetInt32(0));
            }
            return fasesCompletadas;
        }
        public int ObtenerSegundosPorFase(int actividadId, int faseId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT COALESCE(SUM(MinutosEfectivos), 0) FROM RegistrosEsfuerzo WHERE ActividadId = @actividadId AND FaseId = @faseId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);
            comando.Parameters.AddWithValue("@faseId", faseId);

            object resultado = comando.ExecuteScalar() ?? 0;
            return Convert.ToInt32(resultado);
        }
        public List<RegistroEsfuerzo> ObtenerPorActividad(int actividadId)
        {
            var lista = new List<RegistroEsfuerzo>();
            using var conexion = ConexionDB.ObtenerConexion();

            string query = @"
        SELECT Id, ActividadId, FaseId, FechaInicio, FechaFin, MinutosEfectivos
        FROM RegistrosEsfuerzo
        WHERE ActividadId = @ActividadId
        ORDER BY FechaInicio ASC;";

            using var comando = new SqliteCommand(query, conexion);
            comando.Parameters.AddWithValue("@ActividadId", actividadId);
            using var reader = comando.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new RegistroEsfuerzo
                {
                    Id = reader.GetInt32(0),
                    ActividadId = reader.GetInt32(1),
                    FaseId = reader.GetInt32(2),
                    FechaInicio = DateTime.Parse(reader.GetString(3)),
                    FechaFin = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    MinutosEfectivos = reader.GetInt32(5)
                });
            }

            return lista;
        }
    }
}