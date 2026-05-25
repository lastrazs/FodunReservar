using FodunReservas.Business.DTOs;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Business.Interfaces.Services;

namespace FodunReservas.Business.Services;

public class DisponibilidadService(IAlojamientoRepository alojamientoRepository) : IDisponibilidadService
{
    public async Task<IReadOnlyList<DisponibilidadResult>> ConsultarAsync(
        DisponibilidadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FechaSalida <= request.FechaEntrada)
        {
            throw new ArgumentException("La fecha de salida debe ser mayor que la fecha de entrada.");
        }

        var alojamientos = await alojamientoRepository.ObtenerDisponiblesAsync(
            request.FechaEntrada,
            request.FechaSalida,
            request.SedeId,
            request.NroPersonas,
            cancellationToken);

        return alojamientos
            .Select(a => new DisponibilidadResult
            {
                AlojamientoId = a.Id,
                NumeroAlojamiento = a.NumeroAlojamiento,
                TipoAlojamiento = a.TipoAlojamiento?.Nombre ?? string.Empty,
                Descripcion = a.Descripcion,
                CapacidadMaxima = a.CapacidadMaxima,
                NumeroHabitaciones = a.NumeroHabitaciones,
                TieneBano = a.TieneBano,
                TieneCocineta = a.TieneCocineta,
                TieneTelevision = a.TieneTelevision,
                TieneNevera = a.TieneNevera,
                TieneTerraza = a.TieneTerraza,
                TieneSalaEstar = a.TieneSalaEstar,
                TieneParqueadero = a.TieneParqueadero
            })
            .ToList();
    }
}
