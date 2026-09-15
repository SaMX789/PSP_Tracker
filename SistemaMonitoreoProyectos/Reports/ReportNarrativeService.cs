namespace SistemaMonitoreoProyectos.Services
{
    public static class ReportNarrativeService
    {
        public static string ObtenerDiagnosticoEstimacion(double desviacionPct, double factorCalibracion, bool esCompletado, double consumoPct)
        {
            if (!esCompletado)
            {
                return $"Proyecto actualmente EN CURSO. Se ha consumido el {consumoPct:0.0}% del tiempo planeado. Las métricas finales de precisión se calcularán al marcar la actividad como completada.";
            }

            return desviacionPct switch
            {
                <= -20.0 => $"Sobreestimación amplia ({desviacionPct:0.0}%). Se reservó capacidad en exceso respecto al esfuerzo real.",
                > -20.0 and <= 10.0 => "Estimación madura y confiable. El equipo demuestra un entendimiento preciso de la complejidad tecnológica.",
                > 10.0 and <= 30.0 => $"Tendencia a la subestimación (+{desviacionPct:0.0}%). Aplicar Factor de Calibración de {factorCalibracion:0.00}x.",
                _ => $"Subestimación crítica (+{desviacionPct:0.0}%). Brecha severa entre la percepción inicial y la complejidad real."
            };
        }

        public static string ObtenerDiagnosticoCalidad(double porcentajeRetrabajo, int defectosFugaSevera)
        {
            string textoBase = porcentajeRetrabajo switch
            {
                0.0 => "Sin retrabajo registrado hasta el momento.",
                <= 5.0 => $"Calidad alta. Únicamente el {porcentajeRetrabajo:0.0}% del tiempo se ha dedicado a correcciones.",
                > 5.0 and <= 15.0 => $"Tasa de retrabajo dentro del margen tolerable ({porcentajeRetrabajo:0.0}%).",
                _ => $"Alerta de Mantenimiento: El {porcentajeRetrabajo:0.0}% del tiempo trabajado se ha gastado en corregir errores."
            };

            if (defectosFugaSevera > 0)
            {
                textoBase += $" Se han detectado {defectosFugaSevera} defectos con Fuga Severa (>2 fases).";
            }

            return textoBase;
        }

        public static string ObtenerDictamenFinal(double factorCalibracion, double porcentajeRetrabajo, bool esCompletado)
        {
            if (!esCompletado)
                return "Estado: EN EJECUCIÓN. Monitoreo de tiempo y calidad activo en el widget.";

            if (factorCalibracion <= 1.15 && porcentajeRetrabajo <= 10.0)
                return "Dictamen: PROYECTO SALUDABLE. Datos aptos para integrar la línea base de productividad interna.";

            if (porcentajeRetrabajo > 15.0)
                return "Dictamen: REQUIERE ACCIÓN CORRECTIVA. Reforzar listas de verificación en 'Design Review'.";

            return $"Dictamen: CALIBRACIÓN REQUERIDA. Incorporar multiplicador {factorCalibracion:0.00}x en planificación.";
        }
    }
}