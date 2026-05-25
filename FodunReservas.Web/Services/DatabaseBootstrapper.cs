using System.Text.RegularExpressions;
using FodunReservas.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Web.Services;

public class DatabaseBootstrapper(
    FodunReservasDbContext context,
    IWebHostEnvironment environment,
    ILogger<DatabaseBootstrapper> logger)
{
    private static readonly string[] SeedPrefixes =
    [
        "INSERT INTO Sedes",
        "INSERT INTO TipoAlojamiento",
        "INSERT INTO Alojamiento",
        "INSERT INTO Habitacion",
        "INSERT INTO Temporada",
        "INSERT INTO Tarifa"
    ];

    private readonly FodunReservasDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<DatabaseBootstrapper> _logger = logger;

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Crear la base de datos si no existe
            await _context.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo asegurar la creación de la base de datos");
        }

        // Intentar aplicar migraciones pendientes
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Si falla, intentar registrar la migración como aplicada (base de datos existente)
            _logger.LogWarning(ex, "Error al aplicar migraciones inicialmente. Verificando si es una base de datos existente.");
            await MarkMigrationAsAppliedIfNeededAsync(cancellationToken);
            
            // Reintentar las migraciones
            try
            {
                await _context.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Error al reintentar migraciones después de registrar la migración.");
                throw;
            }
        }

        await SeedDomainDataIfNeededAsync(cancellationToken);
        await EnsureStoredProceduresAsync(cancellationToken);
    }

    private async Task MarkMigrationAsAppliedIfNeededAsync(CancellationToken cancellationToken)
    {
        const string migrationId = "20260523033456_InitialIdentityAndDomain";
        
        try
        {
            // Registrar la migración como aplicada sin ejecutarla
            await _context.Database.ExecuteSqlRawAsync(
                $@"IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '{migrationId}')
                   BEGIN
                       INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                       VALUES ('{migrationId}', '8.0.0');
                   END",
                cancellationToken);
            
            _logger.LogInformation("Migración {MigrationId} marcada como aplicada en base de datos existente.", migrationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo marcar la migración como aplicada");
        }
    }

    private async Task SeedDomainDataIfNeededAsync(CancellationToken cancellationToken)
    {
        if (await _context.Sedes.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var scriptPath = Path.Combine(_environment.ContentRootPath, "Bd", "BdScript.sql");
        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("No se encontro el script base de dominio en {Path}.", scriptPath);
            return;
        }

        var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        foreach (var batch in SplitBatches(scriptContent)
                     .Where(batch => SeedPrefixes.Any(prefix => batch.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
        {
            await _context.Database.ExecuteSqlRawAsync(batch, cancellationToken);
        }
    }

    private async Task EnsureStoredProceduresAsync(CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(_environment.ContentRootPath, "Bd", "SPfodun.sql");
        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("No se encontro el script de procedimientos almacenados en {Path}.", scriptPath);
            return;
        }

        var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        foreach (var batch in SplitBatches(scriptContent)
                     .Where(batch => !batch.TrimStart().StartsWith("USE ", StringComparison.OrdinalIgnoreCase)))
        {
            await _context.Database.ExecuteSqlRawAsync(batch, cancellationToken);
        }
    }

    private static IEnumerable<string> SplitBatches(string script)
    {
        return Regex
            .Split(script, @"^\s*GO\s*$(\r?\n)?", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(batch => batch.Trim())
            .Where(batch => !string.IsNullOrWhiteSpace(batch));
    }
}
