namespace Matricula.Infrastructure.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }
    public int Port { get; init; } = 25;
    public bool UseSsl { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public required string SenderEmail { get; init; }
    public string SenderName { get; init; } = "Sistema de Matrícula";
}
