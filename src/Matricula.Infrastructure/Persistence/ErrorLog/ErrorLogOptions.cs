namespace Matricula.Infrastructure.Persistence.ErrorLog;

public class ErrorLogOptions
{
    public const string SectionName = "Errores";

    /// <summary>Días que se conserva un registro de error antes de que la depuración lo elimine (RN-49).</summary>
    public int DiasRetencion { get; init; } = 90;
}
