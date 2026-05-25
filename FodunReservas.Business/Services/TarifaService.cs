using FodunReservas.Business.DTOs;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Business.Interfaces.Services;

namespace FodunReservas.Business.Services;

public class TarifaService(ITarifaRepository tarifaRepository) : ITarifaService
{
    public async Task<ConsultaTarifaResult?> ConsultarAsync(
        ConsultaTarifaRequest request,
        CancellationToken cancellationToken = default)
    {
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

        var personasAdicionales = Math.Max(0, request.NroPersonas - tarifa.PersonasBase);
        var valorAdicional = personasAdicionales * tarifa.ValorPersonaAdicional;

        return new ConsultaTarifaResult
        {
            TarifaId = tarifa.Id,
            Temporada = tarifa.Temporada?.Nombre ?? string.Empty,
            NumeroHabitaciones = tarifa.NumeroHabitaciones,
            PersonasBase = tarifa.PersonasBase,
            ValorNoche = tarifa.ValorNoche,
            ValorPersonaAdicional = tarifa.ValorPersonaAdicional,
            PersonasAdicionales = personasAdicionales,
            ValorAdicional = valorAdicional
        };
    }
}
