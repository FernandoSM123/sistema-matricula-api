namespace Matricula.Infrastructure.Seed;

/// <summary>
/// Datos del primer administrador. Los datos personales son genéricos y se corrigen después (RF-55);
/// la temporal va solo en user-secrets o variables de entorno.
/// </summary>
public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Correo { get; init; } = "admin@instituto.local";
    public string Cedula { get; init; } = "000000000";
    public string Nombre { get; init; } = "John";
    public string Apellidos { get; init; } = "Doe";
    public string Telefono { get; init; } = "00000000";
    public string? ContrasenaTemporal { get; init; }
}