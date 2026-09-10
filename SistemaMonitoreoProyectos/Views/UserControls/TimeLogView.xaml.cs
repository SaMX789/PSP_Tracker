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
            TextoRutaTimeLog.Text = $"/ {actividad.Proyecto.ToUpper()} / TIME LOG";
            var fases = _faseRepo.ObtenerTodas().ToDictionary(f => f.Id, f => f.Nombre);
            var registros = _esfuerzoRepo.ObtenerPorActividad(actividadId);

            List<TimeLogItemDTO> listaDTO = new List<TimeLogItemDTO>();
            int acumuladoSegundos = 0;

            foreach (var reg in registros)
            {
                acumuladoSegundos += reg.MinutosEfectivos;
                int minEfectivos = reg.MinutosEfectivos / 60;
                int minAcumulados = acumuladoSegundos / 60;

                listaDTO.Add(new TimeLogItemDTO
                {
                    FechaFormatted = reg.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                    Fase = fases.ContainsKey(reg.FaseId) ? fases[reg.FaseId] : "General",
                    Descripcion = actividad.Descripcion,
                    MinutosEfectivos = minEfectivos,
                    TotalAcumuladoFormatted = $"{minAcumulados} min"
                });
            }

            ListaRegistrosTiempo.ItemsSource = listaDTO;
            TextoTiempoTotalTimeLog.Text = $"{acumuladoSegundos / 60} min";
        }
    }
}