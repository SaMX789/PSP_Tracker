using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class TimeLogView : UserControl
    {
        private readonly RegistroEsfuerzoRepository _esfuerzoRepo = new RegistroEsfuerzoRepository();
        private readonly FaseRepository _faseRepo = new FaseRepository();
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private readonly InterrupcionRepository _interrupcionRepo = new InterrupcionRepository();
        public TimeLogView()
        {
            InitializeComponent();
        }
        public void CargarRegistrosTiempo(int actividadId)
        {
            var actividad = _actividadRepo.ObtenerPorId(actividadId);
            if (actividad == null) return;

            // 1. Llenar Tarjeta de Encabezado
            TextoRutaTimeLog.Text = $"/ {actividad.Proyecto.ToUpper()} / TIME LOG";
            TextoTituloActividad.Text = actividad.Proyecto;
            TextoDescripcionActividad.Text = string.IsNullOrWhiteSpace(actividad.Descripcion) ? "Sin descripción registrada" : actividad.Descripcion;
            TextoResponsable.Text = $"👤 Responsable: {actividad.Responsable}";

            double hrsEstimadas = Math.Round(actividad.TiempoEstimadoMinutos / 60.0, 1);
            TextoTiempoEstimado.Text = $"⏱ Estimado: {hrsEstimadas} hrs ({actividad.TiempoEstimadoMinutos} min)";

            // 2. Cargar Registros de Esfuerzo e Interrupciones
            var fases = _faseRepo.ObtenerTodas().ToDictionary(f => f.Id, f => f.Nombre);
            var registros = _esfuerzoRepo.ObtenerPorActividad(actividadId);
            var mapaInterrupciones = _interrupcionRepo.ObtenerConteoInterrupcionesPorActividad(actividadId);

            List<TimeLogItemDTO> listaDTO = new List<TimeLogItemDTO>();
            int acumuladoSegundos = 0;

            foreach (var reg in registros)
            {
                int segundosTramo = reg.MinutosEfectivos;
                acumuladoSegundos += segundosTramo;

                TimeSpan tTramo = TimeSpan.FromSeconds(segundosTramo);
                TimeSpan tAcumulado = TimeSpan.FromSeconds(acumuladoSegundos);

                // Formateo mm:ss o hh:mm:ss
                string formatoTramo = tTramo.TotalHours >= 1
                    ? tTramo.ToString(@"hh\:mm\:ss")
                    : tTramo.ToString(@"mm\:ss");

                string formatoAcumulado = tAcumulado.TotalHours >= 1
                    ? $"{(int)tAcumulado.TotalHours:00}:{tAcumulado.Minutes:00}:{tAcumulado.Seconds:00}"
                    : $"{tAcumulado.Minutes:00}:{tAcumulado.Seconds:00}";

                int numInterrupciones = mapaInterrupciones.ContainsKey(reg.Id) ? mapaInterrupciones[reg.Id] : 0;

                listaDTO.Add(new TimeLogItemDTO
                {
                    FechaFormatted = reg.FechaInicio.ToString("dd/MM/yyyy HH:mm:ss"),
                    Fase = fases.ContainsKey(reg.FaseId) ? fases[reg.FaseId] : "General",
                    DuracionFormatted = formatoTramo,
                    InterrupcionesFormatted = numInterrupciones.ToString(),
                    TotalAcumuladoFormatted = formatoAcumulado
                });
            }

            // 3. Asignar datos a la lista y pie de página
            ListaRegistrosTiempo.ItemsSource = listaDTO;

            TimeSpan totalTiempo = TimeSpan.FromSeconds(acumuladoSegundos);
            int totalMinutos = acumuladoSegundos / 60;
            TextoTiempoTotalTimeLog.Text = $"{(int)totalTiempo.TotalHours:00}:{totalTiempo.Minutes:00}:{totalTiempo.Seconds:00} ({totalMinutos} min)";
        }
        private void ListaRegistrosTiempo_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ListaRegistrosTiempo.View is GridView gridView)
            {
                // 35 píxeles de margen para asegurar que no aparezca scroll horizontal por el scrollbar vertical
                double margenScrollbar = 35;
                double anchoDisponible = ListaRegistrosTiempo.ActualWidth - margenScrollbar;

                if (anchoDisponible > 0)
                {
                    // Dividimos el espacio disponible en porcentajes (la suma total es 1.0 = 100%)
                    gridView.Columns[0].Width = anchoDisponible * 0.25; // FECHA / HORA INICIO (25%)
                    gridView.Columns[1].Width = anchoDisponible * 0.20; // FASE (20%)
                    gridView.Columns[2].Width = anchoDisponible * 0.20; // DURACIÓN TRAMO (20%)
                    gridView.Columns[3].Width = anchoDisponible * 0.15; // INTERRUPCIONES (15%)
                    gridView.Columns[4].Width = anchoDisponible * 0.20; // ACUMULADO (20%)
                }
            }
        }
    }
}