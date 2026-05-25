namespace FodunReservas.Business.DTOs;

public sealed record CalculoReservaRequest(
    int SedeId,
    int AlojamientoId,
    int NroHabitaciones,
    int NroPersonas,
    int NroAcompanantes,
    bool RequiereLavanderia,
    DateOnly FechaEntrada,
    DateOnly FechaSalida);
