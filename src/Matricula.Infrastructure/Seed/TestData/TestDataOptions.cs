using Matricula.Domain.Rules;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Configuración de la siembra de datos de prueba (spec datos-prueba). La contraseña va solo en user-secrets.
/// </summary>
public class TestDataOptions
{
    public const string SectionName = "TestData";

    public bool Enabled { get; init; }

    /// <summary>Con un valor, la siembra se repite igual; sin él, cada siembra es distinta.</summary>
    public int? Seed { get; init; }

    public int Cursos { get; init; } = 8;
    public int Aulas { get; init; } = 5;
    public int Profesores { get; init; } = 6;
    public int Estudiantes { get; init; } = 30;
    public int GruposPerPeriodo { get; init; } = 5;
    public int MaxMatriculasPerEstudiante { get; init; } = 2;

    /// <summary>Contraseña de todas las cuentas de prueba (RN-26).</summary>
    public string? Password { get; init; }

    /// <summary>
    /// Errores de configuración, uno por clave. No se valida al arrancar: solo importa cuando la siembra va a ejecutarse
    /// (plan, decisiones de implementación).
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errores = new List<string>();

        ValidateRange(errores, nameof(Cursos), Cursos, 1, 200);
        ValidateRange(errores, nameof(Aulas), Aulas, 1, 100);
        ValidateRange(errores, nameof(Profesores), Profesores, 1, 200);
        ValidateRange(errores, nameof(Estudiantes), Estudiantes, 0, 5000);
        ValidateRange(errores, nameof(GruposPerPeriodo), GruposPerPeriodo, 0, 200);
        ValidateRange(errores, nameof(MaxMatriculasPerEstudiante), MaxMatriculasPerEstudiante, 0, 6);

        if (!PasswordPolicy.IsValid(Password))
        {
            errores.Add($"La opción '{SectionName}:{nameof(Password)}' es obligatoria y debe cumplir RN-26: "
                + $"{PasswordPolicy.Message} Defínala en user-secrets.");
        }

        return errores;
    }

    private static void ValidateRange(List<string> errores, string clave, int valor, int minimo, int maximo)
    {
        if (valor < minimo || valor > maximo)
        {
            errores.Add($"La opción '{SectionName}:{clave}' debe estar entre {minimo} y {maximo}.");
        }
    }
}
