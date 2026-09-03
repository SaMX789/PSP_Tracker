using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaMonitoreoProyectos.Models
{
    public class Fase
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string MacroFase { get; set; } = string.Empty;
        public int Orden { get; set; }
    }
}
