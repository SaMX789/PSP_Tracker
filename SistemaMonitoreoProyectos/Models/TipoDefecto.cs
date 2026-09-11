namespace SistemaMonitoreoProyectos.Models
{
    public class TipoDefecto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string DisplayComboBox => $"{Nombre}";
    }
}