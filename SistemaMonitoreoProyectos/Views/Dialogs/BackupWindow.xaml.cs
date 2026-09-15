using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using SistemaMonitoreoProyectos.Services;

namespace SistemaMonitoreoProyectos.Views.Dialogs
{
    public partial class BackupWindow : Window
    {
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private readonly BackupService _backupService = new BackupService();
        private readonly List<CheckBox> _listaCheckBoxes = new List<CheckBox>();

        public BackupWindow()
        {
            InitializeComponent();
            Loaded += BackupWindow_Loaded;
        }

        private void BackupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextoRutaBackups.Text = $"📂 {Path.GetFileName(_backupService.ObtenerRutaCarpetaBackups())}/";
            CargarListaActividades();
        }

        private void CargarListaActividades()
        {
            ContenedorActividadesSeleccion.Children.Clear();
            _listaCheckBoxes.Clear();

            var actividades = _actividadRepo.ObtenerTodas();

            foreach (var act in actividades)
            {
                var chk = new CheckBox
                {
                    Content = $"{act.Proyecto} ({act.Responsable})", // Se remueve el ID
                    Tag = act.Id,
                    Foreground = (System.Windows.Media.Brush)FindResource("BrocheTextoPrincipal"),
                    FontSize = 13,
                    Margin = new Thickness(0, 4, 0, 4)
                };

                _listaCheckBoxes.Add(chk);
                ContenedorActividadesSeleccion.Children.Add(chk);
            }
        }

        // Abrir la carpeta de Backups en el Explorador de Windows
        private void AbrirCarpetaBackups_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string ruta = _backupService.ObtenerRutaCarpetaBackups();
                if (System.IO.Directory.Exists(ruta))
                {
                    System.Diagnostics.Process.Start("explorer.exe", ruta);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkSeleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            bool esChequeado = ChkSeleccionarTodos.IsChecked ?? false;
            foreach (var chk in _listaCheckBoxes)
            {
                chk.IsChecked = esChequeado;
            }
        }

        private void BotonExportar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = _listaCheckBoxes
                .Where(c => c.IsChecked == true)
                .Select(c => (int)c.Tag)
                .ToList();

            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Selecciona al menos una actividad para exportar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string rutaGuardada = _backupService.ExportarActividades(seleccionados);
                MessageBox.Show($"Respaldo creado con éxito en:\n\n{rutaGuardada}", "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BotonImportar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos JSON de Backup (*.json)|*.json",
                InitialDirectory = _backupService.ObtenerRutaCarpetaBackups(),
                Title = "Selecciona el archivo de respaldo a importar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int totalImportadas = _backupService.ImportarDesdeJson(openFileDialog.FileName);
                    MessageBox.Show($"Se importaron exitosamente {totalImportadas} actividades.", "Importación Completada", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar el archivo: {ex.Message}", "Error de Importación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BotonCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}