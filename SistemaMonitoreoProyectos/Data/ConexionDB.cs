using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace SistemaMonitoreoProyectos.Data
{
    public static class ConexionDB
    {
        // Se ejecuta automáticamente al primer uso de ConexionDB y almacena la ruta de AppData
        private static readonly string RutaBaseDatos = InicializarBaseDatos();

        public static SqliteConnection ObtenerConexion()
        {
            var conexion = new SqliteConnection($"Data Source={RutaBaseDatos}");
            conexion.Open();

            using var comando = conexion.CreateCommand();
            comando.CommandText = "PRAGMA foreign_keys = ON;";
            comando.ExecuteNonQuery();

            return conexion;
        }

        private static string InicializarBaseDatos()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string carpetaDestino = Path.Combine(localAppData, "PSPTracker", "Data");
            string rutaDestino = Path.Combine(carpetaDestino, "psp_tracker.db");

            if (!File.Exists(rutaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
                string rutaSemilla = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "psp_tracker.db");

                if (File.Exists(rutaSemilla))
                {
                    File.Copy(rutaSemilla, rutaDestino, overwrite: true);
                }
                else
                {
                    throw new FileNotFoundException("No se encontró la plantilla base psp_tracker.db en la carpeta del ejecutable.");
                }
            }

            return rutaDestino;
        }
    }
}