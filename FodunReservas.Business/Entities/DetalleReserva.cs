namespace FodunReservas.Business.Entities;

public class DetalleReserva
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public int AlojamientoId { get; set; }
    public decimal ValorNoche { get; set; }
    public int NroNoches { get; set; }
    public decimal SubTotal { get; set; }
    public Reserva? Reserva { get; set; }
    public Alojamiento? Alojamiento { get; set; }
}
