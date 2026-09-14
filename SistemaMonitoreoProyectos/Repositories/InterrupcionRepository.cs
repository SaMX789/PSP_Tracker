using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class InterrupcionRepository : IInterrupcionRepository
    {
        public void Agregar(Interrupcion interrupcion)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                INSERT INTO Interrupciones (RegistroEsfuerzoId, DuracionMinutos, FechaHora)
                VALUES (@registroEsfuerzoId, @duracionMinutos, @fechaHora);";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@registroEsfuerzoId", interrupcion.RegistroEsfuerzoId);
            comando.Parameters.AddWithValue("@duracionMinutos", interrupcion.DuracionMinutos);
            comando.Parameters.AddWithValue("@fechaHora", Convert.ToDateTime(interrupcion.FechaHora).ToString("yyyy-MM-dd HH:mm:ss"));

            comando.ExecuteNonQuery();
        }

        public int ObtenerSegundosInterrupcionPorActividad(int actividadId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                SELECT COALESCE(SUM(i.DuracionMinutos), 0)
                FROM Interrupciones i
                INNER JOIN RegistrosEsfuerzo r ON i.RegistroEsfuerzoId = r.Id
                WHERE r.ActividadId = @actividadId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            object resultado = comando.ExecuteScalar() ?? 0;
            return Convert.ToInt32(resultado);
        }

        public Dictionary<int, int> ObtenerConteoInterrupcionesPorActividad(int actividadId)
        {
            var resultado = new Dictionary<int, int>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = @"
                SELECT i.RegistroEsfuerzoId, COUNT(*) as Conteo
                FROM Interrupciones i
                INNER JOIN RegistrosEsfuerzo r ON i.RegistroEsfuerzoId = r.Id
                WHERE r.ActividadId = @actividadId
                GROUP BY i.RegistroEsfuerzoId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@actividadId", actividadId);

            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                int registroId = lector.GetInt32(0);
                int conteo = lector.GetInt32(1);
                resultado[registroId] = conteo;
            }

            return resultado;
        }
    }
}