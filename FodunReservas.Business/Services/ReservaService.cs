using FodunReservas.Business.DTOs;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Business.Interfaces.Services;

namespace FodunReservas.Business.Services;

public class ReservaService(ITarifaRepository tarifaRepository) : IReservaService
{
    public async Task<CalculoReservaResult?> CalcularTotalAsync(
        CalculoReservaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FechaSalida <= request.FechaEntrada)
        {
            throw new ArgumentException("La fecha de salida debe ser mayor que la fecha de entrada.");
        }

        if (request.NroPersonas <= 0)
        {
            throw new ArgumentException("El numero de personas debe ser mayor a cero.");
        }

        var tarifa = await tarifaRepository.ObtenerTarifaAplicableAsync(
            request.SedeId,
            request.AlojamientoId,
            request.NroPersonas,
            request.FechaEntrada,
            cancellationToken);

        if (tarifa is null)
        {
            return null;
        }

        var numeroNoches = request.FechaSalida.DayNumber - request.FechaEntrada.DayNumber;
        var personasAdicionales = Math.Max(0, request.NroPersonas - tarifa.PersonasBase);
        var subtotalNoches = tarifa.ValorNoche * numeroNoches;
        var valorAdicionales = personasAdicionales * tarifa.ValorPersonaAdicional * numeroNoches;

        return new CalculoReservaResult
        {
            NumeroNoches = numeroNoches,
            ValorNoche = tarifa.ValorNoche,
            PersonasIncluidas = tarifa.PersonasBase,
            PersonasAdicionales = personasAdicionales,
            ValorPersonaAdicional = tarifa.ValorPersonaAdicional,
            SubtotalNoches = subtotalNoches,
            ValorAdicionales = valorAdicionales,
            TotalReserva = subtotalNoches + valorAdicionales,
            Temporada = tarifa.Temporada?.Nombre ?? string.Empty
        };
    }
}
