using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Views.UserControls
{
    public partial class DefectLogView : UserControl
    {
        private readonly RegistroDefectoRepository _defectoRepo = new RegistroDefectoRepository();
        private readonly FaseRepository _faseRepo = new FaseRepository();
        private readonly TipoDefectoRepository _tipoRepo = new TipoDefectoRepository();
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        public DefectLogView()
        {
            InitializeComponent();
        }

        public void CargarBitacoraDefectos(int actividadId)
        {
            var fases = _faseRepo.ObtenerTodas().ToDictionary(f => (long)f.Id, f => f);
            var tipos = _tipoRepo.ObtenerTodos().ToDictionary(t => t.Id, t => t.Nombre);
            var defectos = _defectoRepo.ObtenerPorActividad(actividadId);

            var actividad = _actividadRepo.ObtenerPorId(actividadId);
            if (actividad != null)
            {
                // Actualiza la ruta dinámica del encabezado
                TextoRutaDefectLog.Text = $"/ {actividad.Proyecto.ToUpper()} / DEFECT LOG";
            }

            List<DefectLogItemDTO> listaDTO = new List<DefectLogItemDTO>();
            long totalSegundosCorreccion = 0;

            foreach (var def in defectos)
            {
                totalSegundosCorreccion += def.TiempoCorreccionMinutos;
                TimeSpan t = TimeSpan.FromSeconds(def.TiempoCorreccionMinutos);

                string nombreOrigen = fases.ContainsKey(def.FaseOrigenId) ? fases[def.FaseOrigenId].Nombre : "N/A";
                string nombreDeteccion = fases.ContainsKey(def.FaseDeteccionId) ? fases[def.FaseDeteccionId].Nombre : "N/A";

                int ordenOrigen = fases.ContainsKey(def.FaseOrigenId) ? fases[def.FaseOrigenId].Orden : 0;
                int ordenDeteccion = fases.ContainsKey(def.FaseDeteccionId) ? fases[def.FaseDeteccionId].Orden : 0;

                int saltoFases = ordenDeteccion - ordenOrigen;
                string impactoTexto = "Inmediato";
                string impactoColor = "#10B981";

                if (saltoFases == 1)
                {
                    impactoTexto = "Fuga Corta (+1)";
                    impactoColor = "#F59E0B";
                }
                else if (saltoFases > 1)
                {
                    impactoTexto = $"Fuga Severa (+{saltoFases})";
                    impactoColor = "#EF4444";
                }

                string nombreTipo = def.TipoDefectoId.HasValue && tipos.ContainsKey(def.TipoDefectoId.Value)
                    ? $"{def.TipoDefectoId.Value} - {tipos[def.TipoDefectoId.Value]}"
                    : "Sin Tipo";

                listaDTO.Add(new DefectLogItemDTO
                {
                    Id = def.Id,
                    DescripcionError = def.DescripcionError,
                    TipoDefectoTexto = nombreTipo,
                    RutaFases = $"{nombreOrigen} ➔ {nombreDeteccion}",
                    ImpactoFugaTexto = impactoTexto,
                    ImpactoFugaColor = impactoColor,
                    TiempoCorreccionFormatted = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}",
                    EstadoTexto = def.EsResuelto == 1 ? "Listo" : "Pendiente",
                    EstadoColor = def.EsResuelto == 1 ? "#10B981" : "#FF4D4D",
                    EsAnidadoVisibility = def.DefectoPadreId.HasValue ? "Visible" : "Collapsed"
                });
            }

            ListaDefectos.ItemsSource = listaDTO;
            TimeSpan tTotal = TimeSpan.FromSeconds(totalSegundosCorreccion);
            TextoTiempoTotalDefectos.Text = $"{(int)tTotal.TotalHours:00}:{tTotal.Minutes:00}:{tTotal.Seconds:00} ({totalSegundosCorreccion / 60} min)";
        }
    }
}