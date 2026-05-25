namespace FodunReservas.Business.DTOs;

public sealed class CalculoReservaResult
{
    public int NumeroNoches { get; set; }
    public decimal ValorNoche { get; set; }
    public int PersonasIncluidas { get; set; }
    public int PersonasAdicionales { get; set; }
    public decimal ValorPersonaAdicional { get; set; }
    public decimal SubtotalNoches { get; set; }
    public decimal ValorAdicionales { get; set; }
    public decimal ValorAcompanantes { get; set; }
    public decimal ValorLavanderia { get; set; }
    public decimal TotalServicios { get; set; }
    public decimal TotalReserva { get; set; }
    public string Temporada { get; set; } = string.Empty;
}
