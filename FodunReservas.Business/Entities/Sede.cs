namespace FodunReservas.Business.Entities;

public class Sede
{
    public int Id { get; set; }
    public string TipoSede { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool TieneServicioLavanderia { get; set; }
    public decimal ValorLavanderia { get; set; }
    public bool PermiteAcompanantes { get; set; }
    public decimal ValorAcompanante { get; set; }
    public bool Activa { get; set; } = true;
    public ICollection<Alojamiento> Alojamientos { get; set; } = new List<Alojamiento>();
    public ICollection<Tarifa> Tarifas { get; set; } = new List<Tarifa>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
