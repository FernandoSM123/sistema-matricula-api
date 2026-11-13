namespace Matricula.Infrastructure.Persistence.Errores;

public class ErroresOptions
{
    public const string SectionName = "Errores";

    /// <summary>Días que se conserva un registro de error antes de que la depuración lo elimine (RN-49).</summary>
    public int DiasRetencion { get; init; } = 90;
}
