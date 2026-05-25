using FodunReservas.Business.Entities;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Data.Repositories;

public class AlojamientoRepository(FodunReservasDbContext context) : IAlojamientoRepository
{
    public async Task<Alojamiento?> ObtenerPorIdAsync(int alojamientoId, CancellationToken cancellationToken = default)
    {
        return await context.Alojamientos
            .AsNoTracking()
            .Include(x => x.Sede)
            .Include(x => x.TipoAlojamiento)
            .FirstOrDefaultAsync(x => x.Id == alojamientoId, cancellationToken);
    }

    public async Task<IReadOnlyList<Alojamiento>> ObtenerDisponiblesAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int sedeId,
        int? nroPersonas = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Alojamientos
            .AsNoTracking()
            .Include(x => x.TipoAlojamiento)
            .Where(x => x.SedeId == sedeId && x.Activo);

        if (nroPersonas.HasValue)
        {
            query = query.Where(x => x.CapacidadMaxima >= nroPersonas.Value);
        }

        query = query.Where(a => !a.DetallesReserva.Any(dr =>
            dr.Reserva != null &&
            (dr.Reserva.Estado == "Pendiente" || dr.Reserva.Estado == "Confirmada") &&
            fechaEntrada < dr.Reserva.FechaSalida &&
            fechaSalida > dr.Reserva.FechaLlegada));

        return await query
            .OrderBy(x => x.NumeroAlojamiento)
            .ToListAsync(cancellationToken);
    }
}
