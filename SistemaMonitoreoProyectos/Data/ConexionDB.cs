using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaMonitoreoProyectos.Data
{
    public static class ConexionDB
    {
        private const string CadenaConexion = "Data Source=Data/psp_tracker.db";

        public static SqliteConnection ObtenerConexion()
        {
            var conexion = new SqliteConnection(CadenaConexion);
            conexion.Open();

            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA foreign_keys = ON;";
            comando.ExecuteNonQuery();

            return conexion;
        }
    }
}
