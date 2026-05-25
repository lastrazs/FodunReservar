using FodunReservas.Business.Entities;

namespace FodunReservas.Business.Interfaces.Repositories;

public interface IAlojamientoRepository
{
    Task<Alojamiento?> ObtenerPorIdAsync(int alojamientoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alojamiento>> ObtenerDisponiblesAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int sedeId,
        int? nroPersonas = null,
        CancellationToken cancellationToken = default);
}
