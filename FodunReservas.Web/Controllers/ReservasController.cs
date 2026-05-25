using System.ComponentModel.DataAnnotations;
using FodunReservas.Business.DTOs;
using FodunReservas.Business.Entities;
using FodunReservas.Data.Context;
using FodunReservas.Data.Identity;
using FodunReservas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Web.Controllers;

[Authorize]
public class ReservasController(
    FodunReservasDbContext context,
    UserManager<ApplicationUser> userManager,
    IReservaQueryService reservaQueryService,
    ILogger<ReservasController> logger) : Controller
{
    private readonly FodunReservasDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IReservaQueryService _reservaQueryService = reservaQueryService;
    private readonly ILogger<ReservasController> _logger = logger;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sedes = await _context.Sedes
            .AsNoTracking()
            .Where(s => s.Activa)
            .OrderBy(s => s.Nombre)
            .Select(s => new SedeResumenViewModel
            {
                Id = s.Id,
                Nombre = s.Nombre,
                TipoSede = s.TipoSede,
                Ubicacion = s.Ubicacion,
                Direccion = s.Direccion,
                Descripcion = s.Descripcion,
                PermiteAcompanantes = s.PermiteAcompanantes,
                TieneServicioLavanderia = s.TieneServicioLavanderia,
                ValorAcompanante = s.ValorAcompanante,
                ValorLavanderia = s.ValorLavanderia
            })
            .ToListAsync(cancellationToken);

        return View(new ReservasIndexViewModel
        {
            Sedes = sedes
        });
    }

    [HttpGet]
    public async Task<IActionResult> Disponibilidad(
        int? sedeId,
        DateOnly? fechaEntrada,
        DateOnly? fechaSalida,
        int? nroPersonas,
        CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var model = new DisponibilidadViewModel
        {
            SedeId = sedeId,
            FechaEntrada = fechaEntrada ?? hoy.AddDays(1),
            FechaSalida = fechaSalida ?? hoy.AddDays(2),
            NroPersonas = nroPersonas ?? 1,
            HasSearched = sedeId.HasValue && fechaEntrada.HasValue && fechaSalida.HasValue
        };

        await CargarSedesAsync(model, cancellationToken);

        if (!model.HasSearched)
        {
            return View(model);
        }

        if (!ValidarConsultaDisponibilidad(model))
        {
            model.ErrorMessage = "Corrige los datos de busqueda para consultar disponibilidad.";
            return View(model);
        }

        try
        {
            model.Resultados = await ConsultarDisponibilidadPorFechaYPersonasAsync(
                model.FechaEntrada!.Value,
                model.FechaSalida!.Value,
                model.NroPersonas,
                model.SedeId!.Value,
                cancellationToken);
        }
        catch
        {
            model.ErrorMessage = "No fue posible consultar la disponibilidad. Verifica que la base y los procedimientos almacenados esten aplicados.";
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Confirmar(
        int sedeId,
        int alojamientoId,
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int nroPersonas,
        CancellationToken cancellationToken)
    {
        var model = await ConstruirConfirmacionAsync(
            sedeId,
            alojamientoId,
            fechaEntrada,
            fechaSalida,
            nroPersonas,
            0,
            false,
            null,
            cancellationToken);

        if (model is null)
        {
            TempData["ReservaError"] = "No se pudo preparar la reserva. Revisa la disponibilidad nuevamente.";
            return RedirectToAction(nameof(Disponibilidad), new
            {
                sedeId,
                fechaEntrada,
                fechaSalida,
                nroPersonas
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(
        ConfirmarReservaViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await ConstruirConfirmacionAsync(
                model.SedeId,
                model.AlojamientoId,
                model.FechaEntrada,
                model.FechaSalida,
                model.NroPersonas,
                model.NroAcompanantes,
                model.RequiereLavanderia,
                model.Observaciones,
                cancellationToken);

            if (invalidModel is null)
            {
                TempData["ReservaError"] = "No fue posible reconstruir la reserva para su confirmacion.";
                return RedirectToAction(nameof(Disponibilidad), new
                {
                    sedeId = model.SedeId,
                    fechaEntrada = model.FechaEntrada,
                    fechaSalida = model.FechaSalida,
                    nroPersonas = model.NroPersonas
                });
            }

            return View(invalidModel);
        }

        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        var confirmacion = await ConstruirConfirmacionAsync(
            model.SedeId,
            model.AlojamientoId,
            model.FechaEntrada,
            model.FechaSalida,
            model.NroPersonas,
            model.NroAcompanantes,
            model.RequiereLavanderia,
            model.Observaciones,
            cancellationToken);

        if (confirmacion is null)
        {
            TempData["ReservaError"] = "La reserva ya no esta disponible o no tiene una tarifa valida. Intenta de nuevo.";
            return RedirectToAction(nameof(Disponibilidad), new
            {
                sedeId = model.SedeId,
                fechaEntrada = model.FechaEntrada,
                fechaSalida = model.FechaSalida,
                nroPersonas = model.NroPersonas
            });
        }

        var reserva = CrearReservaDesdeConfirmacion(usuario.Id, confirmacion);

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["ReservaExitosa"] = $"La reserva #{reserva.Id} fue creada en estado Pendiente.";
        return RedirectToAction(nameof(Pagar), new { id = reserva.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reserva = await _reservaQueryService.ObtenerReservaPropiaAsync(id, usuarioId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!PuedeGestionarse(reserva))
        {
            TempData["ReservaError"] = "Solo puedes editar reservas pendientes con fecha futura.";
            return RedirectToAction(nameof(MisReservas));
        }

        var model = await ConstruirEdicionAsync(reserva, cancellationToken);
        if (model is null)
        {
            TempData["ReservaError"] = "No fue posible cargar la reserva para su edicion.";
            return RedirectToAction(nameof(MisReservas));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarReservaViewModel model, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reserva = await _reservaQueryService.ObtenerReservaPropiaAsync(model.ReservaId, usuarioId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!PuedeGestionarse(reserva))
        {
            TempData["ReservaError"] = "Solo puedes editar reservas pendientes con fecha futura.";
            return RedirectToAction(nameof(MisReservas));
        }

        if (!ModelState.IsValid)
        {
            var invalidEditModel = await ConstruirEdicionAsync(reserva, cancellationToken, model);
            return invalidEditModel is null
                ? RedirectToAction(nameof(MisReservas))
                : View(invalidEditModel);
        }

        var recalculo = await ConstruirConfirmacionAsync(
            model.SedeId,
            model.AlojamientoId,
            model.FechaEntrada,
            model.FechaSalida,
            model.NroPersonas,
            model.NroAcompanantes,
            model.RequiereLavanderia,
            model.Observaciones,
            cancellationToken);

        if (recalculo is null)
        {
            ModelState.AddModelError(string.Empty, "No fue posible recalcular la reserva con los nuevos datos.");
            var failedEditModel = await ConstruirEdicionAsync(reserva, cancellationToken, model);
            return failedEditModel is null
                ? RedirectToAction(nameof(MisReservas))
                : View(failedEditModel);
        }

        ActualizarReserva(reserva, model, recalculo);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["ReservaActualizada"] = $"La reserva #{reserva.Id} fue actualizada correctamente.";
        return RedirectToAction(nameof(MisReservas));
    }

    [HttpGet]
    public async Task<IActionResult> Pagar(int id, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reserva = await _reservaQueryService.ObtenerReservaPropiaAsync(id, usuarioId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!string.Equals(reserva.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ReservaError"] = "Solo las reservas pendientes pueden pasar por el pago simulado.";
            return RedirectToAction(nameof(MisReservas));
        }

        var detalle = reserva.Detalles.FirstOrDefault();
        if (detalle is null)
        {
            TempData["ReservaError"] = "La reserva no tiene detalle para procesar el pago.";
            return RedirectToAction(nameof(MisReservas));
        }

        return View(new PagoSimuladoViewModel
        {
            ReservaId = reserva.Id,
            SedeNombre = reserva.Sede?.Nombre ?? string.Empty,
            NumeroAlojamiento = detalle.Alojamiento?.NumeroAlojamiento ?? string.Empty,
            FechaLlegada = reserva.FechaLlegada,
            FechaSalida = reserva.FechaSalida,
            NroPersonas = reserva.NroPersonas,
            NroAcompanantes = reserva.NroAcompanantes,
            RequiereLavanderia = reserva.RequiereLavanderia,
            TotalReserva = reserva.ValorTotal,
            MetodoPago = "PSE"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(PagoSimuladoViewModel model, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reserva = await _reservaQueryService.ObtenerReservaPropiaAsync(model.ReservaId, usuarioId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.SedeNombre = reserva.Sede?.Nombre ?? model.SedeNombre;
            var detalle = reserva.Detalles.FirstOrDefault();
            model.NumeroAlojamiento = detalle?.Alojamiento?.NumeroAlojamiento ?? model.NumeroAlojamiento;
            model.FechaLlegada = reserva.FechaLlegada;
            model.FechaSalida = reserva.FechaSalida;
            model.NroPersonas = reserva.NroPersonas;
            model.NroAcompanantes = reserva.NroAcompanantes;
            model.RequiereLavanderia = reserva.RequiereLavanderia;
            model.TotalReserva = reserva.ValorTotal;
            return View(model);
        }

        if (!string.Equals(reserva.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ReservaError"] = "La reserva ya no se encuentra pendiente de pago.";
            return RedirectToAction(nameof(MisReservas));
        }

        reserva.Estado = "Confirmada";
        reserva.Observaciones = AppendPaymentTrace(
            reserva.Observaciones,
            model.MetodoPago,
            model.TitularPago,
            model.DocumentoPagador);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["ReservaActualizada"] = $"El pago simulado de la reserva #{reserva.Id} fue aprobado.";
        return RedirectToAction(nameof(MisReservas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reserva = await _reservaQueryService.ObtenerReservaPropiaAsync(id, usuarioId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!PuedeGestionarse(reserva))
        {
            TempData["ReservaError"] = "Solo puedes cancelar reservas pendientes con fecha futura.";
            return RedirectToAction(nameof(MisReservas));
        }

        reserva.Estado = "Cancelada";
        await _context.SaveChangesAsync(cancellationToken);

        TempData["ReservaCancelada"] = $"La reserva #{reserva.Id} fue cancelada.";
        return RedirectToAction(nameof(MisReservas));
    }

    public async Task<IActionResult> MisReservas(CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Challenge();
        }

        var reservas = await _reservaQueryService.ObtenerReservasUsuarioAsync(usuarioId, cancellationToken);

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var model = new MisReservasViewModel
        {
            DocumentoUsuario = User.Identity?.Name ?? string.Empty,
            Reservas = reservas.Select(r => new MisReservaItemViewModel
            {
                ReservaId = r.Id,
                SedeNombre = r.Sede?.Nombre ?? string.Empty,
                Estado = r.Estado,
                FechaReserva = r.FechaReserva,
                FechaLlegada = r.FechaLlegada,
                FechaSalida = r.FechaSalida,
                NroPersonas = r.NroPersonas,
                NroHabitaciones = r.NroHabitaciones,
                NroAcompanantes = r.NroAcompanantes,
                RequiereLavanderia = r.RequiereLavanderia,
                ValorTotal = r.ValorTotal,
                Observaciones = r.Observaciones,
                PuedeGestionarse = string.Equals(r.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase)
                    && r.FechaLlegada > hoy
                    && r.Detalles.Any(),
                PuedePagar = string.Equals(r.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase)
                    && r.Detalles.Any(),
                Detalles = r.Detalles
                    .OrderBy(d => d.Alojamiento?.NumeroAlojamiento)
                    .Select(d => new MisReservaDetalleItemViewModel
                    {
                        AlojamientoId = d.AlojamientoId,
                        NumeroAlojamiento = d.Alojamiento?.NumeroAlojamiento ?? string.Empty,
                        NroNoches = d.NroNoches,
                        ValorNoche = d.ValorNoche,
                        SubTotal = d.SubTotal
                    })
                    .ToList()
            }).ToList()
        };

        return View(model);
    }

    private async Task CargarSedesAsync(DisponibilidadViewModel model, CancellationToken cancellationToken)
    {
        model.Sedes = await _context.Sedes
            .AsNoTracking()
            .Where(s => s.Activa)
            .OrderBy(s => s.Nombre)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Nombre} - {s.Ubicacion}",
                Selected = model.SedeId.HasValue && s.Id == model.SedeId.Value
            })
            .ToListAsync(cancellationToken);
    }

    private bool ValidarConsultaDisponibilidad(DisponibilidadViewModel model)
    {
        if (!model.SedeId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SedeId), "Selecciona una sede.");
        }

        if (!model.FechaEntrada.HasValue)
        {
            ModelState.AddModelError(nameof(model.FechaEntrada), "Ingresa la fecha de entrada.");
        }

        if (!model.FechaSalida.HasValue)
        {
            ModelState.AddModelError(nameof(model.FechaSalida), "Ingresa la fecha de salida.");
        }

        if (model.FechaEntrada.HasValue && model.FechaSalida.HasValue && model.FechaSalida <= model.FechaEntrada)
        {
            ModelState.AddModelError(nameof(model.FechaSalida), "La fecha de salida debe ser mayor a la fecha de entrada.");
        }

        if (model.NroPersonas <= 0)
        {
            ModelState.AddModelError(nameof(model.NroPersonas), "El numero de personas debe ser mayor a cero.");
        }

        return ModelState.IsValid;
    }

    private async Task<ConfirmarReservaViewModel?> ConstruirConfirmacionAsync(
        int sedeId,
        int alojamientoId,
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int nroPersonas,
        int nroAcompanantes,
        bool requiereLavanderia,
        string? observaciones,
        CancellationToken cancellationToken)
    {
        try
        {
            var disponibles = await _reservaQueryService.ConsultarDisponibilidadPorFechaYPersonasAsync(
                fechaEntrada,
                fechaSalida,
                nroPersonas,
                sedeId,
                cancellationToken);

            _logger.LogInformation($"SP retornó {disponibles.Count} alojamientos. Buscando alojamientoId={alojamientoId}");
            foreach (var a in disponibles)
            {
                _logger.LogInformation($"  - Alojamiento: {a.AlojamientoId}, Capacidad: {a.CapacidadMaxima}");
            }

            var alojamiento = disponibles.FirstOrDefault(a => a.AlojamientoId == alojamientoId);
            if (alojamiento is null)
            {
                _logger.LogWarning($"Alojamiento {alojamientoId} NO encontrado en disponibles");
                return null;
            }

            if (alojamiento.CapacidadMaxima < nroPersonas)
            {
                _logger.LogWarning($"Capacidad insuficiente: {alojamiento.CapacidadMaxima} < {nroPersonas}");
                return null;
            }

            var sede = await _context.Sedes
                .AsNoTracking()
                .Where(s => s.Id == sedeId && s.Activa)
                .Select(s => new
                {
                    s.Id,
                    s.Nombre,
                    s.Ubicacion,
                    s.PermiteAcompanantes,
                    s.ValorAcompanante,
                    s.TieneServicioLavanderia,
                    s.ValorLavanderia
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (sede is null)
            {
                return null;
            }

            nroAcompanantes = sede.PermiteAcompanantes
                ? Math.Clamp(nroAcompanantes, 0, 10)
                : 0;
            requiereLavanderia = sede.TieneServicioLavanderia && requiereLavanderia;

            var tarifa = await _reservaQueryService.ConsultarTarifaAsync(
                sedeId,
                alojamientoId,
                nroPersonas,
                fechaEntrada,
                cancellationToken);

            var calculo = await _reservaQueryService.CalcularTotalAsync(
                sedeId,
                alojamientoId,
                alojamiento.NumeroHabitaciones,
                nroPersonas,
                nroAcompanantes,
                requiereLavanderia,
                fechaEntrada,
                fechaSalida,
                cancellationToken);

            if (tarifa is null || calculo is null)
            {
                return null;
            }

            return new ConfirmarReservaViewModel
            {
                SedeId = sede.Id,
                SedeNombre = sede.Nombre,
                SedeUbicacion = sede.Ubicacion,
                AlojamientoId = alojamiento.AlojamientoId,
                NumeroAlojamiento = alojamiento.NumeroAlojamiento,
                TipoAlojamiento = alojamiento.TipoAlojamiento,
                Descripcion = alojamiento.Descripcion,
                PermiteAcompanantes = sede.PermiteAcompanantes,
                TieneServicioLavanderia = sede.TieneServicioLavanderia,
                ValorAcompananteUnidad = sede.ValorAcompanante,
                ValorLavanderiaUnidad = sede.ValorLavanderia,
                CapacidadMaxima = alojamiento.CapacidadMaxima,
                NroHabitaciones = alojamiento.NumeroHabitaciones,
                FechaEntrada = fechaEntrada,
                FechaSalida = fechaSalida,
                NroPersonas = nroPersonas,
                NroAcompanantes = nroAcompanantes,
                RequiereLavanderia = requiereLavanderia,
                NumeroNoches = calculo.NumeroNoches,
                Temporada = tarifa.Temporada,
                PersonasIncluidas = tarifa.PersonasBase,
                PersonasAdicionales = calculo.PersonasAdicionales,
                ValorNoche = calculo.ValorNoche,
                ValorPersonaAdicional = tarifa.ValorPersonaAdicional,
                SubtotalNoches = calculo.SubtotalNoches,
                ValorAdicionales = calculo.ValorAdicionales,
                ValorAcompanantes = calculo.ValorAcompanantes,
                ValorLavanderia = calculo.ValorLavanderia,
                TotalServicios = calculo.TotalServicios,
                TotalReserva = calculo.TotalReserva,
                Observaciones = observaciones
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error en ConstruirConfirmacionAsync: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    private async Task<EditarReservaViewModel?> ConstruirEdicionAsync(
        Reserva reserva,
        CancellationToken cancellationToken,
        EditarReservaViewModel? valoresIngresados = null)
    {
        var detalle = reserva.Detalles.FirstOrDefault();
        if (detalle is null)
        {
            return null;
        }

        var fechaEntrada = valoresIngresados?.FechaEntrada ?? reserva.FechaLlegada;
        var fechaSalida = valoresIngresados?.FechaSalida ?? reserva.FechaSalida;
        var nroPersonas = valoresIngresados?.NroPersonas ?? reserva.NroPersonas;
        var nroAcompanantes = valoresIngresados?.NroAcompanantes ?? reserva.NroAcompanantes;
        var requiereLavanderia = valoresIngresados?.RequiereLavanderia ?? reserva.RequiereLavanderia;
        var observaciones = valoresIngresados?.Observaciones ?? reserva.Observaciones;

        var confirmacion = await ConstruirConfirmacionAsync(
            reserva.SedeId,
            detalle.AlojamientoId,
            fechaEntrada,
            fechaSalida,
            nroPersonas,
            nroAcompanantes,
            requiereLavanderia,
            observaciones,
            cancellationToken);

        if (confirmacion is null)
        {
            return null;
        }

        return new EditarReservaViewModel
        {
            ReservaId = reserva.Id,
            SedeId = confirmacion.SedeId,
            AlojamientoId = confirmacion.AlojamientoId,
            SedeNombre = confirmacion.SedeNombre,
            SedeUbicacion = confirmacion.SedeUbicacion,
            NumeroAlojamiento = confirmacion.NumeroAlojamiento,
            TipoAlojamiento = confirmacion.TipoAlojamiento,
            Descripcion = confirmacion.Descripcion,
            CapacidadMaxima = confirmacion.CapacidadMaxima,
            NroHabitaciones = confirmacion.NroHabitaciones,
            FechaEntrada = confirmacion.FechaEntrada,
            FechaSalida = confirmacion.FechaSalida,
            NroPersonas = confirmacion.NroPersonas,
            NroAcompanantes = confirmacion.NroAcompanantes,
            RequiereLavanderia = confirmacion.RequiereLavanderia,
            NumeroNoches = confirmacion.NumeroNoches,
            Temporada = confirmacion.Temporada,
            PersonasIncluidas = confirmacion.PersonasIncluidas,
            PersonasAdicionales = confirmacion.PersonasAdicionales,
            ValorNoche = confirmacion.ValorNoche,
            ValorPersonaAdicional = confirmacion.ValorPersonaAdicional,
            SubtotalNoches = confirmacion.SubtotalNoches,
            ValorAdicionales = confirmacion.ValorAdicionales,
            ValorAcompanantes = confirmacion.ValorAcompanantes,
            ValorLavanderia = confirmacion.ValorLavanderia,
            TotalServicios = confirmacion.TotalServicios,
            TotalReserva = confirmacion.TotalReserva,
            Observaciones = confirmacion.Observaciones
        };
    }

    private static bool PuedeGestionarse(Reserva reserva)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        return string.Equals(reserva.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase)
            && reserva.FechaLlegada > hoy
            && reserva.Detalles.Any();
    }

    private static Reserva CrearReservaDesdeConfirmacion(string usuarioId, ConfirmarReservaViewModel confirmacion)
    {
        var esTemporadaEspecial = string.Equals(
            confirmacion.Temporada,
            "Especial",
            StringComparison.OrdinalIgnoreCase);

        var reserva = new Reserva
        {
            UsuarioId = usuarioId,
            SedeId = confirmacion.SedeId,
            FechaReserva = DateTime.UtcNow,
            FechaLlegada = confirmacion.FechaEntrada,
            FechaSalida = confirmacion.FechaSalida,
            NroPersonas = confirmacion.NroPersonas,
            NroHabitaciones = confirmacion.NroHabitaciones,
            NroAcompanantes = confirmacion.NroAcompanantes,
            RequiereLavanderia = confirmacion.RequiereLavanderia,
            DiasOrdinarios = esTemporadaEspecial ? 0 : confirmacion.NumeroNoches,
            DiasEspeciales = esTemporadaEspecial ? confirmacion.NumeroNoches : 0,
            ValorTotal = confirmacion.TotalReserva,
            Estado = "Pendiente",
            Observaciones = confirmacion.Observaciones
        };

        reserva.Detalles.Add(new DetalleReserva
        {
            AlojamientoId = confirmacion.AlojamientoId,
            ValorNoche = confirmacion.ValorNoche,
            NroNoches = confirmacion.NumeroNoches,
            SubTotal = confirmacion.TotalReserva
        });

        return reserva;
    }

    private static void ActualizarReserva(
        Reserva reserva,
        EditarReservaViewModel model,
        ConfirmarReservaViewModel recalculo)
    {
        var detalle = reserva.Detalles.First();
        var esTemporadaEspecial = string.Equals(
            recalculo.Temporada,
            "Especial",
            StringComparison.OrdinalIgnoreCase);

        reserva.FechaLlegada = recalculo.FechaEntrada;
        reserva.FechaSalida = recalculo.FechaSalida;
        reserva.NroPersonas = recalculo.NroPersonas;
        reserva.NroHabitaciones = recalculo.NroHabitaciones;
        reserva.NroAcompanantes = recalculo.NroAcompanantes;
        reserva.RequiereLavanderia = recalculo.RequiereLavanderia;
        reserva.Observaciones = model.Observaciones;
        reserva.DiasOrdinarios = esTemporadaEspecial ? 0 : recalculo.NumeroNoches;
        reserva.DiasEspeciales = esTemporadaEspecial ? recalculo.NumeroNoches : 0;
        reserva.ValorTotal = recalculo.TotalReserva;

        detalle.ValorNoche = recalculo.ValorNoche;
        detalle.NroNoches = recalculo.NumeroNoches;
        detalle.SubTotal = recalculo.TotalReserva;
    }

    private Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int sedeId,
        CancellationToken cancellationToken)
    {
        return _reservaQueryService.ConsultarDisponibilidadPorFechaAsync(
            fechaEntrada,
            fechaSalida,
            sedeId,
            cancellationToken);
    }

    private Task<List<DisponibilidadResult>> ConsultarDisponibilidadPorFechaYPersonasAsync(
        DateOnly fechaEntrada,
        DateOnly fechaSalida,
        int nroPersonas,
        int sedeId,
        CancellationToken cancellationToken)
    {
        return _reservaQueryService.ConsultarDisponibilidadPorFechaYPersonasAsync(
            fechaEntrada,
            fechaSalida,
            nroPersonas,
            sedeId,
            cancellationToken);
    }

    private Task<ConsultaTarifaResult?> ConsultarTarifaAsync(
        int sedeId,
        int alojamientoId,
        int nroPersonas,
        DateOnly fechaEntrada,
        CancellationToken cancellationToken)
    {
        return _reservaQueryService.ConsultarTarifaAsync(
            sedeId,
            alojamientoId,
            nroPersonas,
            fechaEntrada,
            cancellationToken);
    }

    private Task<CalculoReservaResult?> CalcularTotalAsync(
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
        return _reservaQueryService.CalcularTotalAsync(
            sedeId,
            alojamientoId,
            nroHabitaciones,
            nroPersonas,
            nroAcompanantes,
            requiereLavanderia,
            fechaEntrada,
            fechaSalida,
            cancellationToken);
    }

    private static string AppendPaymentTrace(
        string? observaciones,
        string metodoPago,
        string titularPago,
        string documentoPagador)
    {
        var trazabilidadPago =
            $"Pago simulado aprobado [{DateTime.Now:yyyy-MM-dd HH:mm}] Metodo: {metodoPago}. Titular: {titularPago}. Documento: {documentoPagador}.";

        return string.IsNullOrWhiteSpace(observaciones)
            ? trazabilidadPago
            : $"{observaciones}{Environment.NewLine}{trazabilidadPago}";
    }
}

public sealed class ReservasIndexViewModel
{
    public IReadOnlyList<SedeResumenViewModel> Sedes { get; init; } = [];
}

public sealed class SedeResumenViewModel
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string TipoSede { get; init; } = string.Empty;
    public string Ubicacion { get; init; } = string.Empty;
    public string? Direccion { get; init; }
    public string? Descripcion { get; init; }
    public bool PermiteAcompanantes { get; init; }
    public bool TieneServicioLavanderia { get; init; }
    public decimal ValorAcompanante { get; init; }
    public decimal ValorLavanderia { get; init; }
}

public sealed class DisponibilidadViewModel
{
    public int? SedeId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FechaEntrada { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FechaSalida { get; set; }

    [Range(1, 20)]
    public int NroPersonas { get; set; } = 1;

    public bool HasSearched { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SelectListItem> Sedes { get; set; } = [];
    public List<DisponibilidadResult> Resultados { get; set; } = [];
}

public sealed class ConfirmarReservaViewModel
{
    [Required]
    public int SedeId { get; set; }

    [Required]
    public int AlojamientoId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly FechaEntrada { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly FechaSalida { get; set; }

    [Range(1, 20)]
    public int NroPersonas { get; set; } = 1;

    [Range(0, 10)]
    public int NroAcompanantes { get; set; }

    public bool RequiereLavanderia { get; set; }

    public string SedeNombre { get; set; } = string.Empty;
    public string SedeUbicacion { get; set; } = string.Empty;
    public string NumeroAlojamiento { get; set; } = string.Empty;
    public string TipoAlojamiento { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool PermiteAcompanantes { get; set; }
    public bool TieneServicioLavanderia { get; set; }
    public decimal ValorAcompananteUnidad { get; set; }
    public decimal ValorLavanderiaUnidad { get; set; }
    public int CapacidadMaxima { get; set; }
    public int NroHabitaciones { get; set; }
    public int NumeroNoches { get; set; }
    public string Temporada { get; set; } = string.Empty;
    public int PersonasIncluidas { get; set; }
    public int PersonasAdicionales { get; set; }
    public decimal ValorNoche { get; set; }
    public decimal ValorPersonaAdicional { get; set; }
    public decimal SubtotalNoches { get; set; }
    public decimal ValorAdicionales { get; set; }
    public decimal ValorAcompanantes { get; set; }
    public decimal ValorLavanderia { get; set; }
    public decimal TotalServicios { get; set; }
    public decimal TotalReserva { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

public sealed class EditarReservaViewModel
{
    [Required]
    public int ReservaId { get; set; }

    [Required]
    public int SedeId { get; set; }

    [Required]
    public int AlojamientoId { get; set; }

    public string SedeNombre { get; set; } = string.Empty;
    public string SedeUbicacion { get; set; } = string.Empty;
    public string NumeroAlojamiento { get; set; } = string.Empty;
    public string TipoAlojamiento { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CapacidadMaxima { get; set; }
    public int NroHabitaciones { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly FechaEntrada { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly FechaSalida { get; set; }

    [Range(1, 20)]
    public int NroPersonas { get; set; } = 1;

    [Range(0, 10)]
    public int NroAcompanantes { get; set; }

    public bool RequiereLavanderia { get; set; }

    public int NumeroNoches { get; set; }
    public string Temporada { get; set; } = string.Empty;
    public int PersonasIncluidas { get; set; }
    public int PersonasAdicionales { get; set; }
    public decimal ValorNoche { get; set; }
    public decimal ValorPersonaAdicional { get; set; }
    public decimal SubtotalNoches { get; set; }
    public decimal ValorAdicionales { get; set; }
    public decimal ValorAcompanantes { get; set; }
    public decimal ValorLavanderia { get; set; }
    public decimal TotalServicios { get; set; }
    public decimal TotalReserva { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

public sealed class PagoSimuladoViewModel
{
    [Required]
    public int ReservaId { get; set; }

    public string SedeNombre { get; set; } = string.Empty;
    public string NumeroAlojamiento { get; set; } = string.Empty;
    public DateOnly FechaLlegada { get; set; }
    public DateOnly FechaSalida { get; set; }
    public int NroPersonas { get; set; }
    public int NroAcompanantes { get; set; }
    public bool RequiereLavanderia { get; set; }
    public decimal TotalReserva { get; set; }

    [Required]
    [StringLength(30)]
    public string MetodoPago { get; set; } = "PSE";

    [Required]
    [StringLength(120)]
    public string TitularPago { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DocumentoPagador { get; set; } = string.Empty;
}

public sealed class MisReservasViewModel
{
    public string DocumentoUsuario { get; init; } = string.Empty;
    public IReadOnlyList<MisReservaItemViewModel> Reservas { get; init; } = [];
}

public sealed class MisReservaItemViewModel
{
    public int ReservaId { get; init; }
    public string SedeNombre { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaReserva { get; init; }
    public DateOnly FechaLlegada { get; init; }
    public DateOnly FechaSalida { get; init; }
    public int NroPersonas { get; init; }
    public int NroHabitaciones { get; init; }
    public int NroAcompanantes { get; init; }
    public bool RequiereLavanderia { get; init; }
    public decimal ValorTotal { get; init; }
    public string? Observaciones { get; init; }
    public bool PuedeGestionarse { get; init; }
    public bool PuedePagar { get; init; }
    public IReadOnlyList<MisReservaDetalleItemViewModel> Detalles { get; init; } = [];
}

public sealed class MisReservaDetalleItemViewModel
{
    public int AlojamientoId { get; init; }
    public string NumeroAlojamiento { get; init; } = string.Empty;
    public int NroNoches { get; init; }
    public decimal ValorNoche { get; init; }
    public decimal SubTotal { get; init; }
}
