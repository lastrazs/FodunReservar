using System.ComponentModel.DataAnnotations;
using FodunReservas.Business.Entities;
using FodunReservas.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Web.Controllers;

[Authorize]
public class AdministracionController(FodunReservasDbContext context) : Controller
{
    private readonly FodunReservasDbContext _context = context;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new AdministracionDashboardViewModel
        {
            TotalSedes = await _context.Sedes.CountAsync(cancellationToken),
            TotalAlojamientos = await _context.Alojamientos.CountAsync(cancellationToken),
            TotalTemporadas = await _context.Temporadas.CountAsync(cancellationToken),
            TotalTarifas = await _context.Tarifas.CountAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Sedes(CancellationToken cancellationToken)
    {
        var sedes = await _context.Sedes
            .AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new SedeAdminItemViewModel
            {
                Id = s.Id,
                Nombre = s.Nombre,
                TipoSede = s.TipoSede,
                Ubicacion = s.Ubicacion,
                Activa = s.Activa,
                PermiteAcompanantes = s.PermiteAcompanantes,
                TieneServicioLavanderia = s.TieneServicioLavanderia
            })
            .ToListAsync(cancellationToken);

        return View(new SedesAdminListViewModel { Sedes = sedes });
    }

    [HttpGet]
    public IActionResult CrearSede()
    {
        return View("SedeForm", new SedeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearSede(SedeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("SedeForm", model);
        }

        var sede = new Sede
        {
            TipoSede = model.TipoSede,
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            Ubicacion = model.Ubicacion,
            Direccion = model.Direccion,
            TieneServicioLavanderia = model.TieneServicioLavanderia,
            ValorLavanderia = model.ValorLavanderia,
            PermiteAcompanantes = model.PermiteAcompanantes,
            ValorAcompanante = model.ValorAcompanante,
            Activa = model.Activa
        };

        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La sede fue creada correctamente.";
        return RedirectToAction(nameof(Sedes));
    }

    [HttpGet]
    public async Task<IActionResult> EditarSede(int id, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FindAsync([id], cancellationToken);
        if (sede is null)
        {
            return NotFound();
        }

        return View("SedeForm", new SedeFormViewModel
        {
            Id = sede.Id,
            TipoSede = sede.TipoSede,
            Nombre = sede.Nombre,
            Descripcion = sede.Descripcion,
            Ubicacion = sede.Ubicacion,
            Direccion = sede.Direccion,
            TieneServicioLavanderia = sede.TieneServicioLavanderia,
            ValorLavanderia = sede.ValorLavanderia,
            PermiteAcompanantes = sede.PermiteAcompanantes,
            ValorAcompanante = sede.ValorAcompanante,
            Activa = sede.Activa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarSede(SedeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("SedeForm", model);
        }

        var sede = await _context.Sedes.FindAsync([model.Id], cancellationToken);
        if (sede is null)
        {
            return NotFound();
        }

        sede.TipoSede = model.TipoSede;
        sede.Nombre = model.Nombre;
        sede.Descripcion = model.Descripcion;
        sede.Ubicacion = model.Ubicacion;
        sede.Direccion = model.Direccion;
        sede.TieneServicioLavanderia = model.TieneServicioLavanderia;
        sede.ValorLavanderia = model.ValorLavanderia;
        sede.PermiteAcompanantes = model.PermiteAcompanantes;
        sede.ValorAcompanante = model.ValorAcompanante;
        sede.Activa = model.Activa;

        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La sede fue actualizada correctamente.";
        return RedirectToAction(nameof(Sedes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoSede(int id, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FindAsync([id], cancellationToken);
        if (sede is null)
        {
            return NotFound();
        }

        sede.Activa = !sede.Activa;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = $"La sede {(sede.Activa ? "fue activada" : "fue desactivada")}.";
        return RedirectToAction(nameof(Sedes));
    }

    public async Task<IActionResult> Alojamientos(CancellationToken cancellationToken)
    {
        var alojamientos = await _context.Alojamientos
            .AsNoTracking()
            .Include(a => a.Sede)
            .Include(a => a.TipoAlojamiento)
            .OrderBy(a => a.Sede!.Nombre)
            .ThenBy(a => a.NumeroAlojamiento)
            .Select(a => new AlojamientoAdminItemViewModel
            {
                Id = a.Id,
                SedeNombre = a.Sede!.Nombre,
                TipoAlojamientoNombre = a.TipoAlojamiento!.Nombre,
                NumeroAlojamiento = a.NumeroAlojamiento,
                CapacidadMaxima = a.CapacidadMaxima,
                NumeroHabitaciones = a.NumeroHabitaciones,
                Activo = a.Activo
            })
            .ToListAsync(cancellationToken);

        return View(new AlojamientosAdminListViewModel { Alojamientos = alojamientos });
    }

    [HttpGet]
    public async Task<IActionResult> CrearAlojamiento(CancellationToken cancellationToken)
    {
        var model = new AlojamientoFormViewModel();
        await CargarCatalogosAlojamientoAsync(model, cancellationToken);
        return View("AlojamientoForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAlojamiento(AlojamientoFormViewModel model, CancellationToken cancellationToken)
    {
        await CargarCatalogosAlojamientoAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("AlojamientoForm", model);
        }

        var alojamiento = new Alojamiento
        {
            SedeId = model.SedeId,
            TipoAlojamientoId = model.TipoAlojamientoId,
            NumeroAlojamiento = model.NumeroAlojamiento,
            Descripcion = model.Descripcion,
            CapacidadMaxima = model.CapacidadMaxima,
            NumeroHabitaciones = model.NumeroHabitaciones,
            TieneBano = model.TieneBano,
            TieneCocineta = model.TieneCocineta,
            TieneTelevision = model.TieneTelevision,
            TieneNevera = model.TieneNevera,
            TieneTerraza = model.TieneTerraza,
            TieneSalaEstar = model.TieneSalaEstar,
            TieneParqueadero = model.TieneParqueadero,
            EsNuevo = model.EsNuevo,
            Activo = model.Activo
        };

        _context.Alojamientos.Add(alojamiento);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "El alojamiento fue creado correctamente.";
        return RedirectToAction(nameof(Alojamientos));
    }

    [HttpGet]
    public async Task<IActionResult> EditarAlojamiento(int id, CancellationToken cancellationToken)
    {
        var alojamiento = await _context.Alojamientos.FindAsync([id], cancellationToken);
        if (alojamiento is null)
        {
            return NotFound();
        }

        var model = new AlojamientoFormViewModel
        {
            Id = alojamiento.Id,
            SedeId = alojamiento.SedeId,
            TipoAlojamientoId = alojamiento.TipoAlojamientoId,
            NumeroAlojamiento = alojamiento.NumeroAlojamiento,
            Descripcion = alojamiento.Descripcion,
            CapacidadMaxima = alojamiento.CapacidadMaxima,
            NumeroHabitaciones = alojamiento.NumeroHabitaciones,
            TieneBano = alojamiento.TieneBano,
            TieneCocineta = alojamiento.TieneCocineta,
            TieneTelevision = alojamiento.TieneTelevision,
            TieneNevera = alojamiento.TieneNevera,
            TieneTerraza = alojamiento.TieneTerraza,
            TieneSalaEstar = alojamiento.TieneSalaEstar,
            TieneParqueadero = alojamiento.TieneParqueadero,
            EsNuevo = alojamiento.EsNuevo,
            Activo = alojamiento.Activo
        };

        await CargarCatalogosAlojamientoAsync(model, cancellationToken);
        return View("AlojamientoForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAlojamiento(AlojamientoFormViewModel model, CancellationToken cancellationToken)
    {
        await CargarCatalogosAlojamientoAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("AlojamientoForm", model);
        }

        var alojamiento = await _context.Alojamientos.FindAsync([model.Id], cancellationToken);
        if (alojamiento is null)
        {
            return NotFound();
        }

        alojamiento.SedeId = model.SedeId;
        alojamiento.TipoAlojamientoId = model.TipoAlojamientoId;
        alojamiento.NumeroAlojamiento = model.NumeroAlojamiento;
        alojamiento.Descripcion = model.Descripcion;
        alojamiento.CapacidadMaxima = model.CapacidadMaxima;
        alojamiento.NumeroHabitaciones = model.NumeroHabitaciones;
        alojamiento.TieneBano = model.TieneBano;
        alojamiento.TieneCocineta = model.TieneCocineta;
        alojamiento.TieneTelevision = model.TieneTelevision;
        alojamiento.TieneNevera = model.TieneNevera;
        alojamiento.TieneTerraza = model.TieneTerraza;
        alojamiento.TieneSalaEstar = model.TieneSalaEstar;
        alojamiento.TieneParqueadero = model.TieneParqueadero;
        alojamiento.EsNuevo = model.EsNuevo;
        alojamiento.Activo = model.Activo;

        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "El alojamiento fue actualizado correctamente.";
        return RedirectToAction(nameof(Alojamientos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoAlojamiento(int id, CancellationToken cancellationToken)
    {
        var alojamiento = await _context.Alojamientos.FindAsync([id], cancellationToken);
        if (alojamiento is null)
        {
            return NotFound();
        }

        alojamiento.Activo = !alojamiento.Activo;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = $"El alojamiento {(alojamiento.Activo ? "fue activado" : "fue desactivado")}.";
        return RedirectToAction(nameof(Alojamientos));
    }

    public async Task<IActionResult> Temporadas(CancellationToken cancellationToken)
    {
        var temporadas = await _context.Temporadas
            .AsNoTracking()
            .OrderBy(t => t.Prioridad)
            .ThenBy(t => t.Nombre)
            .Select(t => new TemporadaAdminItemViewModel
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                Prioridad = t.Prioridad,
                EsEspecial = t.EsEspecial,
                Activa = t.Activa
            })
            .ToListAsync(cancellationToken);

        return View(new TemporadasAdminListViewModel { Temporadas = temporadas });
    }

    [HttpGet]
    public IActionResult CrearTemporada()
    {
        return View("TemporadaForm", new TemporadaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTemporada(TemporadaFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("TemporadaForm", model);
        }

        var temporada = new Temporada
        {
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            MesInicio = model.MesInicio,
            DiaInicio = model.DiaInicio,
            MesFin = model.MesFin,
            DiaFin = model.DiaFin,
            EsEspecial = model.EsEspecial,
            Prioridad = model.Prioridad,
            Activa = model.Activa
        };

        _context.Temporadas.Add(temporada);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La temporada fue creada correctamente.";
        return RedirectToAction(nameof(Temporadas));
    }

    [HttpGet]
    public async Task<IActionResult> EditarTemporada(int id, CancellationToken cancellationToken)
    {
        var temporada = await _context.Temporadas.FindAsync([id], cancellationToken);
        if (temporada is null)
        {
            return NotFound();
        }

        return View("TemporadaForm", new TemporadaFormViewModel
        {
            Id = temporada.Id,
            Nombre = temporada.Nombre,
            Descripcion = temporada.Descripcion,
            MesInicio = temporada.MesInicio,
            DiaInicio = temporada.DiaInicio,
            MesFin = temporada.MesFin,
            DiaFin = temporada.DiaFin,
            EsEspecial = temporada.EsEspecial,
            Prioridad = temporada.Prioridad,
            Activa = temporada.Activa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTemporada(TemporadaFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("TemporadaForm", model);
        }

        var temporada = await _context.Temporadas.FindAsync([model.Id], cancellationToken);
        if (temporada is null)
        {
            return NotFound();
        }

        temporada.Nombre = model.Nombre;
        temporada.Descripcion = model.Descripcion;
        temporada.MesInicio = model.MesInicio;
        temporada.DiaInicio = model.DiaInicio;
        temporada.MesFin = model.MesFin;
        temporada.DiaFin = model.DiaFin;
        temporada.EsEspecial = model.EsEspecial;
        temporada.Prioridad = model.Prioridad;
        temporada.Activa = model.Activa;

        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La temporada fue actualizada correctamente.";
        return RedirectToAction(nameof(Temporadas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoTemporada(int id, CancellationToken cancellationToken)
    {
        var temporada = await _context.Temporadas.FindAsync([id], cancellationToken);
        if (temporada is null)
        {
            return NotFound();
        }

        temporada.Activa = !temporada.Activa;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = $"La temporada {(temporada.Activa ? "fue activada" : "fue desactivada")}.";
        return RedirectToAction(nameof(Temporadas));
    }

    public async Task<IActionResult> Tarifas(CancellationToken cancellationToken)
    {
        var tarifas = await _context.Tarifas
            .AsNoTracking()
            .Include(t => t.Sede)
            .Include(t => t.Temporada)
            .OrderBy(t => t.Sede!.Nombre)
            .ThenBy(t => t.Temporada!.Nombre)
            .ThenBy(t => t.NumeroHabitaciones)
            .Select(t => new TarifaAdminItemViewModel
            {
                Id = t.Id,
                SedeNombre = t.Sede!.Nombre,
                TemporadaNombre = t.Temporada!.Nombre,
                NumeroHabitaciones = t.NumeroHabitaciones,
                PersonasBase = t.PersonasBase,
                ValorNoche = t.ValorNoche,
                ValorPersonaAdicional = t.ValorPersonaAdicional,
                Activa = t.Activa
            })
            .ToListAsync(cancellationToken);

        return View(new TarifasAdminListViewModel { Tarifas = tarifas });
    }

    [HttpGet]
    public async Task<IActionResult> CrearTarifa(CancellationToken cancellationToken)
    {
        var model = new TarifaFormViewModel();
        await CargarCatalogosTarifaAsync(model, cancellationToken);
        return View("TarifaForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTarifa(TarifaFormViewModel model, CancellationToken cancellationToken)
    {
        await CargarCatalogosTarifaAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("TarifaForm", model);
        }

        var tarifa = new Tarifa
        {
            SedeId = model.SedeId,
            TemporadaId = model.TemporadaId,
            NumeroHabitaciones = model.NumeroHabitaciones,
            PersonasBase = model.PersonasBase,
            ValorNoche = model.ValorNoche,
            ValorPersonaAdicional = model.ValorPersonaAdicional,
            Activa = model.Activa
        };

        _context.Tarifas.Add(tarifa);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La tarifa fue creada correctamente.";
        return RedirectToAction(nameof(Tarifas));
    }

    [HttpGet]
    public async Task<IActionResult> EditarTarifa(int id, CancellationToken cancellationToken)
    {
        var tarifa = await _context.Tarifas.FindAsync([id], cancellationToken);
        if (tarifa is null)
        {
            return NotFound();
        }

        var model = new TarifaFormViewModel
        {
            Id = tarifa.Id,
            SedeId = tarifa.SedeId,
            TemporadaId = tarifa.TemporadaId,
            NumeroHabitaciones = tarifa.NumeroHabitaciones,
            PersonasBase = tarifa.PersonasBase,
            ValorNoche = tarifa.ValorNoche,
            ValorPersonaAdicional = tarifa.ValorPersonaAdicional,
            Activa = tarifa.Activa
        };

        await CargarCatalogosTarifaAsync(model, cancellationToken);
        return View("TarifaForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTarifa(TarifaFormViewModel model, CancellationToken cancellationToken)
    {
        await CargarCatalogosTarifaAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("TarifaForm", model);
        }

        var tarifa = await _context.Tarifas.FindAsync([model.Id], cancellationToken);
        if (tarifa is null)
        {
            return NotFound();
        }

        tarifa.SedeId = model.SedeId;
        tarifa.TemporadaId = model.TemporadaId;
        tarifa.NumeroHabitaciones = model.NumeroHabitaciones;
        tarifa.PersonasBase = model.PersonasBase;
        tarifa.ValorNoche = model.ValorNoche;
        tarifa.ValorPersonaAdicional = model.ValorPersonaAdicional;
        tarifa.Activa = model.Activa;

        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "La tarifa fue actualizada correctamente.";
        return RedirectToAction(nameof(Tarifas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoTarifa(int id, CancellationToken cancellationToken)
    {
        var tarifa = await _context.Tarifas.FindAsync([id], cancellationToken);
        if (tarifa is null)
        {
            return NotFound();
        }

        tarifa.Activa = !tarifa.Activa;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = $"La tarifa {(tarifa.Activa ? "fue activada" : "fue desactivada")}.";
        return RedirectToAction(nameof(Tarifas));
    }

    private async Task CargarCatalogosAlojamientoAsync(AlojamientoFormViewModel model, CancellationToken cancellationToken)
    {
        model.Sedes = await _context.Sedes
            .AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Nombre} - {s.Ubicacion}",
                Selected = s.Id == model.SedeId
            })
            .ToListAsync(cancellationToken);

        model.TiposAlojamiento = await _context.TiposAlojamiento
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Nombre,
                Selected = t.Id == model.TipoAlojamientoId
            })
            .ToListAsync(cancellationToken);
    }

    private async Task CargarCatalogosTarifaAsync(TarifaFormViewModel model, CancellationToken cancellationToken)
    {
        model.Sedes = await _context.Sedes
            .AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Nombre} - {s.Ubicacion}",
                Selected = s.Id == model.SedeId
            })
            .ToListAsync(cancellationToken);

        model.Temporadas = await _context.Temporadas
            .AsNoTracking()
            .OrderBy(t => t.Prioridad)
            .ThenBy(t => t.Nombre)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.Nombre} (prioridad {t.Prioridad})",
                Selected = t.Id == model.TemporadaId
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class AdministracionDashboardViewModel
{
    public int TotalSedes { get; init; }
    public int TotalAlojamientos { get; init; }
    public int TotalTemporadas { get; init; }
    public int TotalTarifas { get; init; }
}

public sealed class SedesAdminListViewModel
{
    public IReadOnlyList<SedeAdminItemViewModel> Sedes { get; init; } = [];
}

public sealed class SedeAdminItemViewModel
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string TipoSede { get; init; } = string.Empty;
    public string Ubicacion { get; init; } = string.Empty;
    public bool Activa { get; init; }
    public bool PermiteAcompanantes { get; init; }
    public bool TieneServicioLavanderia { get; init; }
}

public sealed class SedeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TipoSede { get; set; } = "Sede Recreativa";

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    [StringLength(100)]
    public string Ubicacion { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Direccion { get; set; }

    public bool TieneServicioLavanderia { get; set; }
    public decimal ValorLavanderia { get; set; }
    public bool PermiteAcompanantes { get; set; }
    public decimal ValorAcompanante { get; set; }
    public bool Activa { get; set; } = true;
}

public sealed class AlojamientosAdminListViewModel
{
    public IReadOnlyList<AlojamientoAdminItemViewModel> Alojamientos { get; init; } = [];
}

public sealed class AlojamientoAdminItemViewModel
{
    public int Id { get; init; }
    public string SedeNombre { get; init; } = string.Empty;
    public string TipoAlojamientoNombre { get; init; } = string.Empty;
    public string NumeroAlojamiento { get; init; } = string.Empty;
    public int CapacidadMaxima { get; init; }
    public int NumeroHabitaciones { get; init; }
    public bool Activo { get; init; }
}

public sealed class AlojamientoFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int SedeId { get; set; }

    [Required]
    public int TipoAlojamientoId { get; set; }

    [Required]
    [StringLength(10)]
    public string NumeroAlojamiento { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(1, 100)]
    public int CapacidadMaxima { get; set; }

    [Range(1, 20)]
    public int NumeroHabitaciones { get; set; } = 1;

    public bool TieneBano { get; set; }
    public bool TieneCocineta { get; set; }
    public bool TieneTelevision { get; set; }
    public bool TieneNevera { get; set; }
    public bool TieneTerraza { get; set; }
    public bool TieneSalaEstar { get; set; }
    public bool TieneParqueadero { get; set; }
    public bool EsNuevo { get; set; }
    public bool Activo { get; set; } = true;

    public IReadOnlyList<SelectListItem> Sedes { get; set; } = [];
    public IReadOnlyList<SelectListItem> TiposAlojamiento { get; set; } = [];
}

public sealed class TemporadasAdminListViewModel
{
    public IReadOnlyList<TemporadaAdminItemViewModel> Temporadas { get; init; } = [];
}

public sealed class TemporadaAdminItemViewModel
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int Prioridad { get; init; }
    public bool EsEspecial { get; init; }
    public bool Activa { get; init; }
}

public sealed class TemporadaFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [Range(1, 12)]
    public int? MesInicio { get; set; }

    [Range(1, 31)]
    public int? DiaInicio { get; set; }

    [Range(1, 12)]
    public int? MesFin { get; set; }

    [Range(1, 31)]
    public int? DiaFin { get; set; }

    public bool EsEspecial { get; set; }

    [Range(1, 999)]
    public int Prioridad { get; set; } = 10;

    public bool Activa { get; set; } = true;
}

public sealed class TarifasAdminListViewModel
{
    public IReadOnlyList<TarifaAdminItemViewModel> Tarifas { get; init; } = [];
}

public sealed class TarifaAdminItemViewModel
{
    public int Id { get; init; }
    public string SedeNombre { get; init; } = string.Empty;
    public string TemporadaNombre { get; init; } = string.Empty;
    public int NumeroHabitaciones { get; init; }
    public int PersonasBase { get; init; }
    public decimal ValorNoche { get; init; }
    public decimal ValorPersonaAdicional { get; init; }
    public bool Activa { get; init; }
}

public sealed class TarifaFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int SedeId { get; set; }

    [Required]
    public int TemporadaId { get; set; }

    [Range(1, 20)]
    public int NumeroHabitaciones { get; set; } = 1;

    [Range(1, 20)]
    public int PersonasBase { get; set; } = 4;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ValorNoche { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ValorPersonaAdicional { get; set; }

    public bool Activa { get; set; } = true;

    public IReadOnlyList<SelectListItem> Sedes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Temporadas { get; set; } = [];
}
