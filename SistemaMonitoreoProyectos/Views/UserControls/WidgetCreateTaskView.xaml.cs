using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    /// <summary>
    /// Lógica de interacción para WidgetCreateTaskView.xaml
    /// </summary>
    public partial class WidgetCreateTaskView : UserControl
    {
        private readonly ActividadRepository _actividadRepository;
        public WidgetCreateTaskView()
        {
            InitializeComponent();
            _actividadRepository = new ActividadRepository();
        }

        private void BotonSiguiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TextoTituloActividad.Text))
                {
                    MessageBox.Show("Por favor, ingresa el título de la actividad.", "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string tiempoInput = TextoTiempoEstimado.Text.Replace(',', '.');
                if (!double.TryParse(tiempoInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double horasEstimadas) || horasEstimadas <= 0)
                {
                    MessageBox.Show("Ingresa un tiempo estimado válido en horas (ejemplo: 1.5).", "Tiempo Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int minutosEstimados = (int)Math.Round(horasEstimadas * 60);

                var nuevaActividad = new Actividad
                {
                    Proyecto = TextoTituloActividad.Text.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(TextoDescripcionActividad.Text)
                        ? TextoTituloActividad.Text.Trim()
                        : TextoDescripcionActividad.Text.Trim(),
                    Responsable = string.IsNullOrWhiteSpace(TextoResponsable.Text)
                        ? "Usuario"
                        : TextoResponsable.Text.Trim(),
                    TiempoEstimadoMinutos = minutosEstimados,
                    FechaInicio = DateTime.Now,
                    Estado = 0 // 0 = En Curso
                };

                int nuevoId = _actividadRepository.Agregar(nuevaActividad);

                var estadoSesionRepository = new EstadoSesionRepository();
                estadoSesionRepository.GuardarOSustituirSesion(new EstadoSesion
                {
                    ActividadId = nuevoId,
                    FaseActualId = 1, 
                    EstadoCronometro = 0, 
                    MinutosAcumulados = 0
                });

                if (Window.GetWindow(this) is WidgetWindow widgetWindow)
                {
                    widgetWindow.CargarTimeActividadCreada();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la nueva actividad:\n\n{ex.Message}", "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaListaTareas();
            }
        }

        private void BotonOpcionesWidget_Click(object sender, RoutedEventArgs e)
        {
            if (BotonOpcionesWidget.ContextMenu != null)
            {
                BotonOpcionesWidget.ContextMenu.IsOpen = true;
            }
        }

        private void MenuItemSalirWidget_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.Close();
            }
        }
    }
}
