using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class TimeLogView : UserControl
    {
        private readonly RegistroEsfuerzoRepository _esfuerzoRepo = new RegistroEsfuerzoRepository();
        private readonly FaseRepository _faseRepo = new FaseRepository();
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();

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

            // 2. Cargar Registros de Esfuerzo
            var fases = _faseRepo.ObtenerTodas().ToDictionary(f => f.Id, f => f.Nombre);
            var registros = _esfuerzoRepo.ObtenerPorActividad(actividadId);

            List<TimeLogItemDTO> listaDTO = new List<TimeLogItemDTO>();
            int acumuladoSegundos = 0;

            foreach (var reg in registros)
            {
                int segundosTramo = reg.MinutosEfectivos;
                acumuladoSegundos += segundosTramo;

                TimeSpan tTramo = TimeSpan.FromSeconds(segundosTramo);
                TimeSpan tAcumulado = TimeSpan.FromSeconds(acumuladoSegundos);

                // Formateo limpio hh:mm:ss o mm:ss
                string formatoTramo = tTramo.TotalHours >= 1
                    ? tTramo.ToString(@"hh\:mm\:ss")
                    : tTramo.ToString(@"mm\:ss");

                string formatoAcumulado = tAcumulado.TotalHours >= 1
                    ? $"{(int)tAcumulado.TotalHours:00}:{tAcumulado.Minutes:00}:{tAcumulado.Seconds:00}"
                    : $"{tAcumulado.Minutes:00}:{tAcumulado.Seconds:00}";

                listaDTO.Add(new TimeLogItemDTO
                {
                    FechaFormatted = reg.FechaInicio.ToString("dd/MM/yyyy HH:mm:ss"),
                    Fase = fases.ContainsKey(reg.FaseId) ? fases[reg.FaseId] : "General",
                    DuracionFormatted = formatoTramo, // Solo muestra mm:ss o hh:mm:ss (Sin el paréntesis de segundos)
                    TotalAcumuladoFormatted = formatoAcumulado
                });
            }

            // 3. Asignar datos a la lista y pie de página
            ListaRegistrosTiempo.ItemsSource = listaDTO;

            TimeSpan totalTiempo = TimeSpan.FromSeconds(acumuladoSegundos);
            int totalMinutos = acumuladoSegundos / 60;
            TextoTiempoTotalTimeLog.Text = $"{(int)totalTiempo.TotalHours:00}:{totalTiempo.Minutes:00}:{totalTiempo.Seconds:00} ({totalMinutos} min)";
        }
    }
}