using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SistemaMonitoreoProyectos.Data;
using SistemaMonitoreoProyectos.Models;
using SistemaMonitoreoProyectos.Repositories;

namespace SistemaMonitoreoProyectos.Services
{
    public class BackupService
    {
        private readonly ActividadRepository _actividadRepo = new ActividadRepository();
        private readonly PlanFaseRepository _planRepo = new PlanFaseRepository();
        private readonly RegistroEsfuerzoRepository _esfuerzoRepo = new RegistroEsfuerzoRepository();
        private readonly RegistroDefectoRepository _defectoRepo = new RegistroDefectoRepository();
        private readonly InterrupcionRepository _interrupcionRepo = new InterrupcionRepository();

        public string ObtenerRutaCarpetaBackups()
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
            return ruta;
        }

        public string ExportarActividades(List<int> actividadesIds)
        {
            var paqueteBackup = new List<ActividadBackupDTO>();

            foreach (int actId in actividadesIds)
            {
                var act = _actividadRepo.ObtenerPorId(actId);
                if (act == null) continue;

                var dto = new ActividadBackupDTO
                {
                    Actividad = act,
                    Planes = _planRepo.ObtenerPorActividad(actId),
                    Esfuerzos = _esfuerzoRepo.ObtenerPorActividad(actId),
                    Defectos = _defectoRepo.ObtenerPorActividad(actId)
                };

                // Obtener interrupciones vinculadas a los esfuerzos de esta actividad
                var esfuerzoIds = dto.Esfuerzos.Select(e => e.Id).ToList();
                var interrupciones = new List<Interrupcion>();

                using (var con = ConexionDB.ObtenerConexion())
                {
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT Id, RegistroEsfuerzoId, DuracionMinutos, FechaHora FROM Interrupciones;";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int regId = reader.GetInt32(1);
                        if (esfuerzoIds.Contains(regId))
                        {
                            interrupciones.Add(new Interrupcion
                            {
                                Id = reader.GetInt32(0),
                                RegistroEsfuerzoId = regId,
                                DuracionMinutos = reader.GetInt32(2),
                                FechaHora = DateTime.Parse(reader.GetString(3))
                            });
                        }
                    }
                }

                dto.Interrupciones = interrupciones;
                paqueteBackup.Add(dto);
            }

            string json = JsonSerializer.Serialize(paqueteBackup, new JsonSerializerOptions { WriteIndented = true });
            string nombreArchivo = $"Backup_PSP_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string rutaCompleta = Path.Combine(ObtenerRutaCarpetaBackups(), nombreArchivo);

            File.WriteAllText(rutaCompleta, json);
            return rutaCompleta;
        }

        public int ImportarDesdeJson(string rutaArchivoJson)
        {
            if (!File.Exists(rutaArchivoJson)) return 0;

            string json = File.ReadAllText(rutaArchivoJson);
            var paquete = JsonSerializer.Deserialize<List<ActividadBackupDTO>>(json);
            if (paquete == null || paquete.Count == 0) return 0;

            int importadasCount = 0;

            foreach (var dto in paquete)
            {
                // 1. Insertar Actividad con nuevo ID
                var nuevaActividad = new Actividad
                {
                    Proyecto = $"{dto.Actividad.Proyecto} (Importado)",
                    Descripcion = dto.Actividad.Descripcion,
                    Responsable = dto.Actividad.Responsable,
                    TiempoEstimadoMinutos = dto.Actividad.TiempoEstimadoMinutos,
                    FechaInicio = dto.Actividad.FechaInicio,
                    FechaCierre = dto.Actividad.FechaCierre,
                    Estado = dto.Actividad.Estado
                };

                int nuevoActividadId = _actividadRepo.Agregar(nuevaActividad);

                // 2. Insertar Planes por Fase
                if (dto.Planes != null)
                {
                    foreach (var plan in dto.Planes)
                    {
                        plan.ActividadId = nuevoActividadId;
                    }
                    _planRepo.GuardarOActualizarLista(dto.Planes);
                }

                // 3. Mapear e Insertar Esfuerzos
                var mapaEsfuerzosIds = new Dictionary<int, int>();
                if (dto.Esfuerzos != null)
                {
                    foreach (var esf in dto.Esfuerzos)
                    {
                        int idViejo = esf.Id;
                        esf.ActividadId = nuevoActividadId;
                        long nuevoEsfuerzoId = _esfuerzoRepo.Agregar(esf);
                        mapaEsfuerzosIds[idViejo] = (int)nuevoEsfuerzoId;
                    }
                }

                // 4. Re-vincular e Insertar Interrupciones
                if (dto.Interrupciones != null)
                {
                    foreach (var inter in dto.Interrupciones)
                    {
                        if (mapaEsfuerzosIds.ContainsKey(inter.RegistroEsfuerzoId))
                        {
                            inter.RegistroEsfuerzoId = mapaEsfuerzosIds[inter.RegistroEsfuerzoId];
                            _interrupcionRepo.Agregar(inter);
                        }
                    }
                }

                // 5. Mapear e Insertar Defectos y Subdefectos
                if (dto.Defectos != null)
                {
                    var mapaDefectosIds = new Dictionary<long, long>();

                    // Primero insertar los defectos raíz (DefectoPadreId == null)
                    foreach (var def in dto.Defectos.Where(d => d.DefectoPadreId == null))
                    {
                        long idViejo = def.Id;
                        def.ActividadId = nuevoActividadId;
                        long nuevoDefectoId = _defectoRepo.Agregar(def);
                        mapaDefectosIds[idViejo] = nuevoDefectoId;
                    }

                    // Luego insertar subdefectos
                    foreach (var def in dto.Defectos.Where(d => d.DefectoPadreId != null))
                    {
                        if (mapaDefectosIds.ContainsKey(def.DefectoPadreId!.Value))
                        {
                            def.ActividadId = nuevoActividadId;
                            def.DefectoPadreId = mapaDefectosIds[def.DefectoPadreId.Value];
                            _defectoRepo.Agregar(def);
                        }
                    }
                }

                importadasCount++;
            }

            return importadasCount;
        }
    }
}