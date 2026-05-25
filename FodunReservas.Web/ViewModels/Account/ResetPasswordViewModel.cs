using System.ComponentModel.DataAnnotations;

namespace FodunReservas.Web.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contrasena")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contrasena.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "La confirmacion no coincide.")]
    [Display(Name = "Confirmar nueva contrasena")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
