namespace FodunReservas.Business.DTOs;

public sealed class DisponibilidadResult
{
    public int AlojamientoId { get; set; }
    public string NumeroAlojamiento { get; set; } = string.Empty;
    public string TipoAlojamiento { get; set; } = string.Empty;
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
}
