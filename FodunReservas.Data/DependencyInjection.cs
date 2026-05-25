using FodunReservas.Business.Interfaces.Repositories;
using FodunReservas.Data.Context;
using FodunReservas.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FodunReservas.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FodunReservasDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(FodunReservasDbContext).Assembly.FullName)));

        services.AddScoped<IAlojamientoRepository, AlojamientoRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();
        services.AddScoped<ITarifaRepository, TarifaRepository>();

        return services;
    }
}
