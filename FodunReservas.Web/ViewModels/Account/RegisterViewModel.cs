using System.ComponentModel.DataAnnotations;

namespace FodunReservas.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El numero de documento es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Numero de documento")]
    public string NroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [Display(Name = "Correo electronico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El celular es obligatorio.")]
    [StringLength(20)]
    public string Celular { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(80)]
    public string? Departamento { get; set; }

    [Required(ErrorMessage = "El municipio es obligatorio.")]
    [StringLength(80)]
    public string Municipio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El barrio es obligatorio.")]
    [StringLength(80)]
    public string Barrio { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Direccion de residencia")]
    public string? DireccionResidencia { get; set; }

    [StringLength(20)]
    [Display(Name = "Telefono de residencia")]
    public string? TelefonoResidencia { get; set; }

    [Display(Name = "Autoriza envio de correos")]
    public bool AutorizaCorreo { get; set; } = true;

    [Display(Name = "Autoriza contacto por celular")]
    public bool AutorizaCelular { get; set; } = true;

    [StringLength(200)]
    [Display(Name = "Pregunta secreta")]
    public string? PreguntaSecreta { get; set; }

    [StringLength(200)]
    [DataType(DataType.Password)]
    [Display(Name = "Respuesta secreta")]
    public string? RespuestaSecreta { get; set; }

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contrasena.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "La confirmacion no coincide.")]
    [Display(Name = "Confirmar contrasena")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
