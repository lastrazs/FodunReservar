namespace FodunReservas.Business.DTOs;

public sealed class ConsultaTarifaResult
{
    public int TarifaId { get; set; }
    public string Temporada { get; set; } = string.Empty;
    public int NumeroHabitaciones { get; set; }
    public int PersonasBase { get; set; }
    public decimal ValorNoche { get; set; }
    public decimal ValorPersonaAdicional { get; set; }
    public int PersonasAdicionales { get; set; }
    public decimal ValorAdicional { get; set; }
}
