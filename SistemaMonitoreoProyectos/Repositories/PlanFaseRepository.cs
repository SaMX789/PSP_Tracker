using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class PlanFaseRepository : IPlanFaseRepository
    {
        public PlanFaseRepository()
        {
            AsegurarTablaPlanFases();
        }

        private void AsegurarTablaPlanFases()
        {
            try
            {
                using var conexion = ConexionDB.ObtenerConexion();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS PlanFases (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ActividadId INTEGER NOT NULL,
                        FaseId INTEGER NOT NULL,
                        TiempoEstimadoMinutos INTEGER NOT NULL DEFAULT 0,
                        DefectosEstimadosInyectados INTEGER NOT NULL DEFAULT 0,
                        DefectosEstimadosRemovidos INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (ActividadId) REFERENCES Actividades(Id) ON DELETE CASCADE,
                        FOREIGN KEY (FaseId) REFERENCES Fases(Id),
                        UNIQUE(ActividadId, FaseId)
                    );";
                using var comando = new SqliteCommand(sql, conexion);
                comando.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al asegurar tabla PlanFases: {ex.Message}");
            }
        }

        public void GuardarOActualizarLista(List<PlanFase> planes)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            using var transaccion = conexion.BeginTransaction();

            string sql = @"
                INSERT INTO PlanFases 
                (ActividadId, FaseId, TiempoEstimadoMinutos, DefectosEstimadosInyectados, DefectosEstimadosRemovidos)
                VALUES 
                (@ActividadId, @FaseId, @TiempoEstimadoMinutos, @DefectosEstimadosInyectados, @DefectosEstimadosRemovidos)
                ON CONFLICT(ActividadId, FaseId) DO UPDATE SET
                    TiempoEstimadoMinutos = excluded.TiempoEstimadoMinutos,
                    DefectosEstimadosInyectados = excluded.DefectosEstimadosInyectados,
                    DefectosEstimadosRemovidos = excluded.DefectosEstimadosRemovidos;";

            foreach (var plan in planes)
            {
                using var comando = new SqliteCommand(sql, conexion, transaccion);
                comando.Parameters.AddWithValue("@ActividadId", plan.ActividadId);
                comando.Parameters.AddWithValue("@FaseId", plan.FaseId);
                comando.Parameters.AddWithValue("@TiempoEstimadoMinutos", plan.TiempoEstimadoMinutos);
                comando.Parameters.AddWithValue("@DefectosEstimadosInyectados", plan.DefectosEstimadosInyectados);
                comando.Parameters.AddWithValue("@DefectosEstimadosRemovidos", plan.DefectosEstimadosRemovidos);
                comando.ExecuteNonQuery();
            }

            transaccion.Commit();
        }

        public List<PlanFase> ObtenerPorActividad(int actividadId)
        {
            var lista = new List<PlanFase>();
            using var conexion = ConexionDB.ObtenerConexion();
            string sql = @"
                SELECT Id, ActividadId, FaseId, TiempoEstimadoMinutos, DefectosEstimadosInyectados, DefectosEstimadosRemovidos
                FROM PlanFases
                WHERE ActividadId = @ActividadId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@ActividadId", actividadId);
            using var reader = comando.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new PlanFase
                {
                    Id = reader.GetInt32(0),
                    ActividadId = reader.GetInt32(1),
                    FaseId = reader.GetInt32(2),
                    TiempoEstimadoMinutos = reader.GetInt32(3),
                    DefectosEstimadosInyectados = reader.GetInt32(4),
                    DefectosEstimadosRemovidos = reader.GetInt32(5)
                });
            }

            return lista;
        }

        public PlanFase? ObtenerPorActividadYFase(int actividadId, int faseId)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            string sql = @"
                SELECT Id, ActividadId, FaseId, TiempoEstimadoMinutos, DefectosEstimadosInyectados, DefectosEstimadosRemovidos
                FROM PlanFases
                WHERE ActividadId = @ActividadId AND FaseId = @FaseId;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@ActividadId", actividadId);
            comando.Parameters.AddWithValue("@FaseId", faseId);
            using var reader = comando.ExecuteReader();

            if (reader.Read())
            {
                return new PlanFase
                {
                    Id = reader.GetInt32(0),
                    ActividadId = reader.GetInt32(1),
                    FaseId = reader.GetInt32(2),
                    TiempoEstimadoMinutos = reader.GetInt32(3),
                    DefectosEstimadosInyectados = reader.GetInt32(4),
                    DefectosEstimadosRemovidos = reader.GetInt32(5)
                };
            }

            return null;
        }
    }
}