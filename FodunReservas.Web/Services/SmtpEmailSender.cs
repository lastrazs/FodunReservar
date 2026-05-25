using System.Net;
using System.Net.Mail;
using System.Text;
using FodunReservas.Web.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace FodunReservas.Web.Services;

public class SmtpEmailSender(IOptions<SmtpSettings> smtpOptions) : IEmailSender
{
    private readonly SmtpSettings _smtpSettings = smtpOptions.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (_smtpSettings.UsePickupDirectory)
        {
            await SaveEmailToPickupDirectoryAsync(email, subject, htmlMessage);
            return;
        }

        ValidateConfiguration();

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        message.To.Add(email);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password)
        };

        await client.SendMailAsync(message);
    }

    private async Task SaveEmailToPickupDirectoryAsync(string email, string subject, string htmlMessage)
    {
        var pickupDirectory = _smtpSettings.PickupDirectory;
        if (string.IsNullOrWhiteSpace(pickupDirectory))
        {
            throw new InvalidOperationException("La carpeta de pickup para correos no esta configurada.");
        }

        Directory.CreateDirectory(pickupDirectory);

        var safeEmail = email.Replace("@", "_at_", StringComparison.OrdinalIgnoreCase)
            .Replace(".", "_", StringComparison.OrdinalIgnoreCase);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeEmail}.html";
        var content = new StringBuilder()
            .AppendLine("<html><body>")
            .AppendLine($"<h2>{subject}</h2>")
            .AppendLine($"<p><strong>Para:</strong> {email}</p>")
            .AppendLine(htmlMessage)
            .AppendLine("</body></html>")
            .ToString();

        await File.WriteAllTextAsync(Path.Combine(pickupDirectory, fileName), content);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host)
            || string.IsNullOrWhiteSpace(_smtpSettings.UserName)
            || string.IsNullOrWhiteSpace(_smtpSettings.Password)
            || string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
        {
            throw new InvalidOperationException("La configuracion SMTP no esta completa.");
        }

        if (_smtpSettings.Password.Contains("CAMBIAR_", StringComparison.OrdinalIgnoreCase)
            || _smtpSettings.UserName.Contains("tudominio.com", StringComparison.OrdinalIgnoreCase)
            || _smtpSettings.FromEmail.Contains("tudominio.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La configuracion SMTP aun contiene valores de ejemplo.");
        }
    }
}
