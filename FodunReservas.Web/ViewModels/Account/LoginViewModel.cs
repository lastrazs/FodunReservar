using System.ComponentModel.DataAnnotations;

namespace FodunReservas.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "El numero de documento es obligatorio.")]
    [Display(Name = "Numero de documento")]
    public string NroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
