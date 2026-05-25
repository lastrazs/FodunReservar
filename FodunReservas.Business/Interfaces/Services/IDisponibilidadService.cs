using FodunReservas.Business.DTOs;

namespace FodunReservas.Business.Interfaces.Services;

public interface IDisponibilidadService
{
    Task<IReadOnlyList<DisponibilidadResult>> ConsultarAsync(
        DisponibilidadRequest request,
        CancellationToken cancellationToken = default);
}
