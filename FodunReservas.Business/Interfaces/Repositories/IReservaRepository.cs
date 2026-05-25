using FodunReservas.Business.Entities;

namespace FodunReservas.Business.Interfaces.Repositories;

public interface IReservaRepository
{
    Task<Reserva> AgregarAsync(Reserva reserva, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
