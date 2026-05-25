using FodunReservas.Business.Entities;

namespace FodunReservas.Business.Interfaces.Repositories;

public interface ITarifaRepository
{
    Task<Tarifa?> ObtenerTarifaAplicableAsync(
        int sedeId,
        int alojamientoId,
        int nroPersonas,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken = default);
}
