namespace FodunReservas.Business.Entities;

public class Tarifa
{
    public int Id { get; set; }
    public int SedeId { get; set; }
    public int TemporadaId { get; set; }
    public int NumeroHabitaciones { get; set; }
    public int PersonasBase { get; set; }
    public decimal ValorNoche { get; set; }
    public decimal ValorPersonaAdicional { get; set; }
    public bool Activa { get; set; } = true;
    public Sede? Sede { get; set; }
    public Temporada? Temporada { get; set; }
}
