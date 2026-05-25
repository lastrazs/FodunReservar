namespace FodunReservas.Business.DTOs;

public sealed record DisponibilidadRequest(
    DateOnly FechaEntrada,
    DateOnly FechaSalida,
    int SedeId,
    int? NroPersonas = null);
