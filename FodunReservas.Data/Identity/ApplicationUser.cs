using Microsoft.AspNetCore.Identity;

namespace FodunReservas.Data.Identity;

public class ApplicationUser : IdentityUser
{
    public string NroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public DateOnly? FechaNacimiento { get; set; }
    public string? Celular { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string? Barrio { get; set; }
    public string? DireccionResidencia { get; set; }
    public string? TelefonoResidencia { get; set; }
    public bool AutorizaCorreo { get; set; } = true;
    public bool AutorizaCelular { get; set; } = true;
    public string? PreguntaSecreta { get; set; }
    public string? RespuestaSecretaHash { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
}
