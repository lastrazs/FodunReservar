namespace FodunReservas.Business.DTOs;

public sealed record ConsultaTarifaRequest(
    int SedeId,
    int AlojamientoId,
    int NroPersonas,
    DateOnly FechaEntrada);
