using System.Security.Cryptography;
using System.Text;
using FodunReservas.Data.Identity;
using FodunReservas.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FodunReservas.Web.Controllers;

[AllowAnonymous]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender emailSender,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await userManager.Users.AnyAsync(x => x.NroDocumento == model.NroDocumento))
        {
            ModelState.AddModelError(nameof(model.NroDocumento), "Ya existe un usuario con ese numero de documento.");
            return View(model);
        }

        if (await userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese correo.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.NroDocumento,
            NroDocumento = model.NroDocumento,
            NombreCompleto = model.NombreCompleto,
            Email = model.Email,
            Celular = model.Celular,
            FechaNacimiento = model.FechaNacimiento,
            Departamento = model.Departamento,
            Municipio = model.Municipio,
            Barrio = model.Barrio,
            DireccionResidencia = model.DireccionResidencia,
            TelefonoResidencia = model.TelefonoResidencia,
            AutorizaCorreo = model.AutorizaCorreo,
            AutorizaCelular = model.AutorizaCelular,
            PreguntaSecreta = model.PreguntaSecreta,
            RespuestaSecretaHash = HashSecretAnswer(model.RespuestaSecreta),
            Activo = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        logger.LogInformation("Usuario registrado con documento {NroDocumento}.", model.NroDocumento);
        TempData["SuccessMessage"] = "Registro exitoso. Ya puede iniciar sesion con su numero de documento.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByNameAsync(model.NroDocumento)
                   ?? await userManager.Users.FirstOrDefaultAsync(x => x.NroDocumento == model.NroDocumento);

        if (user is null || !user.Activo)
        {
            ModelState.AddModelError(string.Empty, "Credenciales invalidas.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            logger.LogInformation("Inicio de sesion exitoso para documento {NroDocumento}.", model.NroDocumento);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "La cuenta esta bloqueada temporalmente por varios intentos fallidos.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Credenciales invalidas.");
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.Users.FirstOrDefaultAsync(x => x.NroDocumento == model.NroDocumento);

        if (user is null || string.IsNullOrWhiteSpace(user.Email) || !user.Activo)
        {
            TempData["SuccessMessage"] = "Si el usuario existe, enviaremos un correo con instrucciones para recuperar la contrasena.";
            return RedirectToAction(nameof(Login));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = user.Email, token = encodedToken },
            Request.Scheme);

        var message = $"""
                       <p>Hola {user.NombreCompleto},</p>
                       <p>Recibimos una solicitud para restablecer la contrasena de su cuenta en <strong>FodunReservas</strong>.</p>
                       <p>Para continuar, haga clic en el siguiente enlace:</p>
                       <p><a href="{callbackUrl}">Restablecer contrasena</a></p>
                       <p>Si usted no realizo esta solicitud, puede ignorar este mensaje.</p>
                       """;

        try
        {
            await emailSender.SendEmailAsync(user.Email, "Recuperacion de contrasena - FodunReservas", message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible enviar el correo de recuperacion para el documento {NroDocumento}.", model.NroDocumento);
            ModelState.AddModelError(string.Empty, "No fue posible enviar el correo de recuperacion. Verifica la configuracion SMTP.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Se envio un correo con instrucciones para recuperar la contrasena.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            TempData["SuccessMessage"] = "La contrasena fue actualizada. Ya puede iniciar sesion.";
            return RedirectToAction(nameof(Login));
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await userManager.ResetPasswordAsync(user, decodedToken, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "La contrasena fue actualizada. Ya puede iniciar sesion con su numero de documento.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private static string? HashSecretAnswer(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        var normalized = answer.Trim().ToUpperInvariant();
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
