using Microsoft.Data.Sqlite;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;
using System;
using System.Collections.Generic;

namespace SistemaMonitoreoProyectos.Repositories
{
    public class TipoDefectoRepository : ITipoDefectoRepository
    {
        public List<TipoDefecto> ObtenerTodos()
        {
            var lista = new List<TipoDefecto>();

            // Usamos el helper de conexión que me pasaste
            using var conexion = ConexionDB.ObtenerConexion();

            string query = "SELECT Id, Nombre, Descripcion FROM TiposDefectos ORDER BY Id";
            using var comando = new SqliteCommand(query, conexion);
            using var reader = comando.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new TipoDefecto
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    // Verificamos nulos por si alguna descripción está vacía en BD
                    Descripcion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                });
            }

            return lista;
        }
    }
}