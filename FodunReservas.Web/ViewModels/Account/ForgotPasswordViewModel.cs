using System.ComponentModel.DataAnnotations;

namespace FodunReservas.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El numero de documento es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Numero de documento")]
    public string NroDocumento { get; set; } = string.Empty;
}
