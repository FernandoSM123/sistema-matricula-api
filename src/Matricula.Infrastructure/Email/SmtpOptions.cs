namespace Matricula.Infrastructure.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }
    public int Port { get; init; } = 25;
    public bool UseSsl { get; init; }
    public string? Usuario { get; init; }
    public string? Contrasena { get; init; }
    public required string Remitente { get; init; }
    public string NombreRemitente { get; init; } = "Sistema de Matrícula";
}
