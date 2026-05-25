using System.Data;
using FodunReservas.Business.DTOs;
using FodunReservas.Business.Entities;
using FodunReservas.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Web.Services;

public interface IReservaQueryService
{
    Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int sedeId,
        CancellationToken cancellationToken);

    Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaYPersonasAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int nroPersonas,
        int sedeId,
        CancellationToken cancellationToken);

    Task<ConsultaTarifaResult?> ConsultarTarifaAsync(
        int sedeId,
        int alojamientoId,
        int nroPersonas,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken);

    Task<CalculoReservaResult?> CalcularTotalAsync(
        int sedeId,
        int alojamientoId,
        int nroHabitaciones,
        int nroPersonas,
        int nroAcompanantes,
        bool requiereLavanderia,
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        CancellationToken cancellationToken);

    Task<Reserva?> ObtenerReservaPropiaAsync(int reservaId, string usuarioId, CancellationToken cancellationToken);
    Task<List<Reserva>> ObtenerReservasUsuarioAsync(string usuarioId, CancellationToken cancellationToken);
}

public class ReservaQueryService(FodunReservasDbContext context) : IReservaQueryService
{
    private readonly FodunReservasDbContext _context = context;

    public Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int sedeId,
        CancellationToken cancellationToken)
    {
        return _context.DisponibilidadResultados
            .FromSqlRaw(
                "EXEC sp_HabitacionesDisponiblesPorFecha @FechaEntrada, @FechaSalida, @SedeId",
                CrearParametroFecha("@FechaEntrada", fechaEntrada),
                CrearParametroFecha("@FechaSalida", fechaSalida),
                new SqlParameter("@SedeId", sedeId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaYPersonasAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int nroPersonas,
        int sedeId,
        CancellationToken cancellationToken)
    {
        return _context.DisponibilidadResultados
            .FromSqlRaw(
                "EXEC sp_HabitacionesDisponiblesPorFechaYPersonas @FechaEntrada, @FechaSalida, @NroPersonas, @SedeId",
                CrearParametroFecha("@FechaEntrada", fechaEntrada),
                CrearParametroFecha("@FechaSalida", fechaSalida),
                new SqlParameter("@NroPersonas", nroPersonas),
                new SqlParameter("@SedeId", sedeId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsultaTarifaResult?> ConsultarTarifaAsync(
        int sedeId,
        int alojamientoId,
        int nroPersonas,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken)
    {
        var resultados = await _context.ConsultaTarifaResultados
            .FromSqlRaw(
                "EXEC sp_ConsultarTarifas @SedeId, @AlojamientoId, @NroPersonas, @FechaEntrada",
                new SqlParameter("@SedeId", sedeId),
                new SqlParameter("@AlojamientoId", alojamientoId),
                new SqlParameter("@NroPersonas", nroPersonas),
                CrearParametroFecha("@FechaEntrada", fechaEntrada))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return resultados.FirstOrDefault();
    }

    public async Task<CalculoReservaResult?> CalcularTotalAsync(
        int sedeId,
        int alojamientoId,
        int nroHabitaciones,
        int nroPersonas,
        int nroAcompanantes,
        bool requiereLavanderia,
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        CancellationToken cancellationToken)
    {
        var resultados = await _context.CalculoReservaResultados
            .FromSqlRaw(
                "EXEC sp_CalcularTotalReserva @SedeId, @AlojamientoId, @NroHabitaciones, @NroPersonas, @NroAcompanantes, @RequiereLavanderia, @FechaEntrada, @FechaSalida",
                new SqlParameter("@SedeId", sedeId),
                new SqlParameter("@AlojamientoId", alojamientoId),
                new SqlParameter("@NroHabitaciones", nroHabitaciones),
                new SqlParameter("@NroPersonas", nroPersonas),
                new SqlParameter("@NroAcompanantes", nroAcompanantes),
                new SqlParameter("@RequiereLavanderia", requiereLavanderia),
                CrearParametroFecha("@FechaEntrada", fechaEntrada),
                CrearParametroFecha("@FechaSalida", fechaSalida))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return resultados.FirstOrDefault();
    }

    public Task<Reserva?> ObtenerReservaPropiaAsync(int reservaId, string usuarioId, CancellationToken cancellationToken)
    {
        return _context.Reservas
            .Include(r => r.Sede)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Alojamiento)
            .FirstOrDefaultAsync(
                r => r.Id == reservaId && r.UsuarioId == usuarioId,
                cancellationToken);
    }

    public Task<List<Reserva>> ObtenerReservasUsuarioAsync(string usuarioId, CancellationToken cancellationToken)
    {
        return _context.Reservas
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .Include(r => r.Sede)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Alojamiento)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync(cancellationToken);
    }

    private static SqlParameter CrearParametroFecha(string nombre, DateOnly fecha)
    {
        return new SqlParameter(nombre, SqlDbType.Date)
        {
            Value = fecha.ToDateTime(TimeOnly.MinValue)
        };
    }
}
