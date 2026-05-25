using FodunReservas.Business.Entities;
using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Data.Context;

namespace FodunReservas.Data.Repositories;

public class ReservaRepository(FodunReservasDbContext context) : IReservaRepository
{
    public async Task<Reserva> AgregarAsync(Reserva reserva, CancellationToken cancellationToken = default)
    {
        await context.Reservas.AddAsync(reserva, cancellationToken);
        return reserva;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
