namespace FodunReservas.Web.Configuration;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public bool UsePickupDirectory { get; set; }
    public string PickupDirectory { get; set; } = "App_Data/Emails";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
