namespace FodunReservas.Business.Entities;

public class Alojamiento
{
    public int Id { get; set; }
    public int SedeId { get; set; }
    public int TipoAlojamientoId { get; set; }
    public string NumeroAlojamiento { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CapacidadMaxima { get; set; }
    public int NumeroHabitaciones { get; set; }
    public bool TieneBano { get; set; }
    public bool TieneCocineta { get; set; }
    public bool TieneTelevision { get; set; }
    public bool TieneNevera { get; set; }
    public bool TieneTerraza { get; set; }
    public bool TieneSalaEstar { get; set; }
    public bool TieneParqueadero { get; set; }
    public bool EsNuevo { get; set; }
    public bool Activo { get; set; } = true;
    public Sede? Sede { get; set; }
    public TipoAlojamiento? TipoAlojamiento { get; set; }
    public ICollection<Habitacion> Habitaciones { get; set; } = new List<Habitacion>();
    public ICollection<DetalleReserva> DetallesReserva { get; set; } = new List<DetalleReserva>();
}
