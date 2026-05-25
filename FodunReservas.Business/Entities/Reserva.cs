namespace FodunReservas.Business.Entities;

public class Reserva
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public int SedeId { get; set; }
    public DateTime FechaReserva { get; set; } = DateTime.UtcNow;
    public DateOnly FechaLlegada { get; set; }
    public DateOnly FechaSalida { get; set; }
    public int NroPersonas { get; set; }
    public int NroHabitaciones { get; set; }
    public int NroAcompanantes { get; set; }
    public bool RequiereLavanderia { get; set; }
    public int DiasOrdinarios { get; set; }
    public int DiasEspeciales { get; set; }
    public decimal ValorTotal { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public Sede? Sede { get; set; }
    public ICollection<DetalleReserva> Detalles { get; set; } = new List<DetalleReserva>();
}
