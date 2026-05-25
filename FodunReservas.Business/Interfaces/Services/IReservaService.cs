using FodunReservas.Business.DTOs;

namespace FodunReservas.Business.Interfaces.Services;

public interface IReservaService
{
    Task<CalculoReservaResult?> CalcularTotalAsync(
        CalculoReservaRequest request,
        CancellationToken cancellationToken = default);
}
