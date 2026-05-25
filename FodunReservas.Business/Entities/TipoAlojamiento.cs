namespace FodunReservas.Business.Entities;

public class TipoAlojamiento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public ICollection<Alojamiento> Alojamientos { get; set; } = new List<Alojamiento>();
}
