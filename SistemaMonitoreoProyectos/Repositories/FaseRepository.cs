using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class FaseRepository : IFaseRepository
    {
        public List<Fase> ObtenerTodas()
        {
            var lista = new List<Fase>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT Id, Nombre, MacroFase, Orden FROM Fases ORDER BY Orden ASC;";

            using var comando = new SqliteCommand(sql, conexion);
            using var reader = comando.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Fase
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    MacroFase = reader.GetString(2),
                    Orden = reader.GetInt32(3)
                });
            }
            return lista;
        }

        public Fase? ObtenerPorId(int id)
        {
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT Id, Nombre, MacroFase, Orden FROM Fases WHERE Id = @id;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            using var reader = comando.ExecuteReader();
            if (reader.Read())
            {
                return new Fase
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    MacroFase = reader.GetString(2),
                    Orden = reader.GetInt32(3)
                };
            }
            return null;
        }

        public List<Fase> ObtenerPorMacroFase(string macroFase)
        {
            var lista = new List<Fase>();
            using var conexion = ConexionDB.ObtenerConexion();
            var sql = "SELECT Id, Nombre, MacroFase, Orden FROM Fases WHERE MacroFase = @macroFase ORDER BY Orden ASC;";

            using var comando = new SqliteCommand(sql, conexion);
            comando.Parameters.AddWithValue("@macroFase", macroFase);

            using var reader = comando.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Fase
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    MacroFase = reader.GetString(2),
                    Orden = reader.GetInt32(3)
                });
            }
            return lista;
        }
    }
}