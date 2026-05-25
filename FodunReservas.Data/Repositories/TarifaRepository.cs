using FodunReservas.Business.Entities;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Data.Repositories;

public class TarifaRepository(FodunReservasDbContext context) : ITarifaRepository
{
    public async Task<Tarifa?> ObtenerTarifaAplicableAsync(
        int sedeId,
        int alojamientoId,
        int nroPersonas,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken = default)
    {
        var alojamiento = await context.Alojamientos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == alojamientoId && x.SedeId == sedeId, cancellationToken);

        if (alojamiento is null)
        {
            return null;
        }

        var sede = await context.Sedes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sedeId, cancellationToken);

        if (sede is null)
        {
            return null;
        }

        var temporada = await ResolverTemporadaAsync(sede, fechaEntrada, cancellationToken);
        if (temporada is null)
        {
            return null;
        }

        return await context.Tarifas
            .AsNoTracking()
            .Include(x => x.Temporada)
            .Where(x =>
                x.SedeId == sedeId &&
                x.TemporadaId == temporada.Id &&
                x.NumeroHabitaciones == alojamiento.NumeroHabitaciones &&
                x.Activa)
            .OrderBy(x => x.PersonasBase > nroPersonas ? 1 : 0)
            .ThenByDescending(x => x.PersonasBase <= nroPersonas ? x.PersonasBase : -1)
            .ThenBy(x => x.PersonasBase)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Temporada?> ResolverTemporadaAsync(
        Sede sede,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken)
    {
        var temporadas = await context.Temporadas
            .AsNoTracking()
            .Where(x => x.Activa)
            .OrderBy(x => x.Prioridad)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (temporadas.Count == 0)
        {
            return null;
        }

        var configuradaPorRango = temporadas
            .Where(x => x.MesInicio.HasValue
                        && x.DiaInicio.HasValue
                        && x.MesFin.HasValue
                        && x.DiaFin.HasValue)
            .FirstOrDefault(x => EstaEnRangoConfigurado(x, fechaEntrada));

        if (configuradaPorRango is not null)
        {
            return configuradaPorRango;
        }

        if (sede.TipoSede == "Apartamento" && sede.Ubicacion.Contains("Santa Marta", StringComparison.OrdinalIgnoreCase))
        {
            return EsTemporadaAltaVacacional(fechaEntrada)
                ? temporadas.FirstOrDefault(x => x.Nombre == "Alta") ?? temporadas.FirstOrDefault(x => x.Nombre == "Baja")
                : temporadas.FirstOrDefault(x => x.Nombre == "Baja") ?? temporadas.FirstOrDefault(x => x.Nombre == "Alta");
        }

        if (EsTemporadaAltaVacacional(fechaEntrada))
        {
            return temporadas.FirstOrDefault(x => x.Nombre == "Alta")
                   ?? temporadas.FirstOrDefault(x => x.Nombre == "Ordinaria");
        }

        if (fechaEntrada.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday or DayOfWeek.Thursday)
        {
            return temporadas.FirstOrDefault(x => x.EsEspecial)
                   ?? temporadas.FirstOrDefault(x => x.Nombre == "Ordinaria");
        }

        return temporadas.FirstOrDefault(x => x.Nombre == "Ordinaria")
               ?? temporadas.FirstOrDefault(x => !x.EsEspecial);
    }

    private static bool EsTemporadaAltaVacacional(DateOnly fecha)
    {
        var mesDia = fecha.Month * 100 + fecha.Day;
        return mesDia >= 615 && mesDia <= 731
               || mesDia >= 1215
               || mesDia <= 115;
    }

    private static bool EstaEnRangoConfigurado(Temporada temporada, DateOnly fecha)
    {
        if (!temporada.MesInicio.HasValue
            || !temporada.DiaInicio.HasValue
            || !temporada.MesFin.HasValue
            || !temporada.DiaFin.HasValue)
        {
            return false;
        }

        var valorActual = fecha.Month * 100 + fecha.Day;
        var inicio = temporada.MesInicio.Value * 100 + temporada.DiaInicio.Value;
        var fin = temporada.MesFin.Value * 100 + temporada.DiaFin.Value;

        return inicio <= fin
            ? valorActual >= inicio && valorActual <= fin
            : valorActual >= inicio || valorActual <= fin;
    }
}
