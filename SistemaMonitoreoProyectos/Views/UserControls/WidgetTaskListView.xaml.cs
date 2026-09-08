using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class WidgetTaskListView : UserControl
    {
        private readonly ActividadRepository _actividadRepository;
        private List<Actividad> _todasLasActividades = new List<Actividad>();

        public WidgetTaskListView()
        {
            InitializeComponent();
            _actividadRepository = new ActividadRepository();

            Loaded += WidgetTaskListView_Loaded;
        }

        private void WidgetTaskListView_Loaded(object sender, RoutedEventArgs e)
        {
            CargarActividades();
        }

        private void CargarActividades()
        {
            try
            {
                _todasLasActividades = _actividadRepository.ObtenerTodas();
                RenderizarLista(_todasLasActividades);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las tareas:\n{ex.Message}", "Error de SQLite", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderizarLista(List<Actividad> lista)
        {
            ContenedorListaTareas.Children.Clear();

            if (lista.Count == 0)
            {
                ContenedorListaTareas.Children.Add(new TextBlock
                {
                    Text = "No se encontraron actividades.",
                    Foreground = (Brush)FindResource("BrocheTextoSecundario"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var actividad in lista)
            {
                ContenedorListaTareas.Children.Add(CrearTarjetaActividad(actividad));
            }
        }
        private Border CrearTarjetaActividad(Actividad actividad)
        {
            bool esTerminada = actividad.Estado == 1;

            var tarjeta = new Border
            {
                Style = (Style)FindResource("EstiloTarjetaBase"),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14, 14, 14, 14),
                Opacity = esTerminada ? 0.7 : 1.0,
                Cursor = Cursors.Hand
            };
            tarjeta.MouseLeftButtonDown += (s, e) =>
            {
                var sesionRepo = new EstadoSesionRepository();
                var registroRepo = new RegistroEsfuerzoRepository();

                // Consultar en qué fase se quedó esta actividad previamente
                int ultimaFase = registroRepo.ObtenerUltimaFasePorActividad(actividad.Id);

                sesionRepo.GuardarOSustituirSesion(new EstadoSesion
                {
                    ActividadId = actividad.Id,
                    FaseActualId = ultimaFase, 
                    EstadoCronometro = 0,
                    MinutosAcumulados = 0
                });

                if (Window.GetWindow(this) is WidgetWindow widgetWindow)
                {
                    widgetWindow.CargarTimeActividadCreada();
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var txtTitulo = new TextBlock
            {
                Text = actividad.Proyecto,
                Foreground = (Brush)FindResource("BrocheTextoPrincipal"),
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Margin = new Thickness(0, 0, 90, 4)
            };
            if (esTerminada) txtTitulo.TextDecorations = TextDecorations.Strikethrough;
            Grid.SetRow(txtTitulo, 0);

            var badge = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Background = (Brush)FindResource(esTerminada ? "BrocheEstadoTerminadoFondo" : "BrocheEstadoEnCursoFondo")
            };
            var txtBadge = new TextBlock
            {
                Text = esTerminada ? "✓ TERMINADA" : "EN CURSO",
                Foreground = (Brush)FindResource(esTerminada ? "BrocheEstadoTerminadoTexto" : "BrocheEstadoEnCursoTexto"),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            badge.Child = txtBadge;
            Grid.SetRow(badge, 0);


            var txtDetalle = new TextBlock
            {
                Text = $"Resp: {actividad.Responsable} • {actividad.Descripcion}",
                Foreground = (Brush)FindResource("BrocheTextoSecundario"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(txtDetalle, 1);

            double horas = Math.Round(actividad.TiempoEstimadoMinutos / 60.0, 1);
            var txtTiempo = new TextBlock
            {
                Text = $"⏱ Estimado: {horas}h ({actividad.TiempoEstimadoMinutos} min)",
                Foreground = (Brush)FindResource("BrocheTextoSecundario"),
                FontSize = 12
            };
            Grid.SetRow(txtTiempo, 2);

            grid.Children.Add(txtTitulo);
            grid.Children.Add(badge);
            grid.Children.Add(txtDetalle);
            grid.Children.Add(txtTiempo);

            tarjeta.Child = grid;
            return tarjeta;
        }
        // Filtrado dinámico en tiempo real
        private void TextoBuscadorActividad_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TextoBuscadorActividad.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro) || filtro == "🔍 buscar actividades...")
            {
                RenderizarLista(_todasLasActividades);
            }
            else
            {
                var filtradas = _todasLasActividades
                    .Where(a => a.Proyecto.ToLower().Contains(filtro) ||
                                a.Responsable.ToLower().Contains(filtro) ||
                                a.Descripcion.ToLower().Contains(filtro))
                    .ToList();

                RenderizarLista(filtradas);
            }
        }
        private void BotonFlotanteNuevaTarea_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is WidgetWindow widgetWindow)
            {
                widgetWindow.CargarVistaCrearTarea();
            }
        }
    }
}