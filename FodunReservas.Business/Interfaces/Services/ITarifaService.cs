using FodunReservas.Business.DTOs;

namespace FodunReservas.Business.Interfaces.Services;

public interface ITarifaService
{
    Task<ConsultaTarifaResult?> ConsultarAsync(
        ConsultaTarifaRequest request,
        CancellationToken cancellationToken = default);
}
