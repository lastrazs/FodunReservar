namespace FodunReservas.Business.Entities;

public class Habitacion
{
    public int Id { get; set; }
    public int AlojamientoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? CapacidadReferencial { get; set; }
    public bool TieneBanoPrivado { get; set; }
    public string? Observaciones { get; set; }
    public bool Activa { get; set; } = true;
    public Alojamiento? Alojamiento { get; set; }
}
