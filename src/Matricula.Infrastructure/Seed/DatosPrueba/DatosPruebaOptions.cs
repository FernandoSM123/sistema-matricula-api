using Matricula.Domain.Reglas;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Configuración de la siembra de datos de prueba (spec datos-prueba). La contraseña va solo en user-secrets.
/// </summary>
public class DatosPruebaOptions
{
    public const string SectionName = "DatosPrueba";

    public bool Habilitado { get; init; }

    /// <summary>Con un valor, la siembra se repite igual; sin él, cada siembra es distinta.</summary>
    public int? Semilla { get; init; }

    public int Cursos { get; init; } = 8;
    public int Aulas { get; init; } = 5;
    public int Profesores { get; init; } = 6;
    public int Estudiantes { get; init; } = 30;
    public int GruposPorPeriodo { get; init; } = 5;
    public int MaxMatriculasPorEstudiante { get; init; } = 2;

    /// <summary>Contraseña de todas las cuentas de prueba (RN-26).</summary>
    public string? Contrasena { get; init; }

    /// <summary>
    /// Errores de configuración, uno por clave. No se valida al arrancar: solo importa cuando la siembra va a ejecutarse
    /// (plan, decisiones de implementación).
    /// </summary>
    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        ValidarRango(errores, nameof(Cursos), Cursos, 1, 200);
        ValidarRango(errores, nameof(Aulas), Aulas, 1, 100);
        ValidarRango(errores, nameof(Profesores), Profesores, 1, 200);
        ValidarRango(errores, nameof(Estudiantes), Estudiantes, 0, 5000);
        ValidarRango(errores, nameof(GruposPorPeriodo), GruposPorPeriodo, 0, 200);
        ValidarRango(errores, nameof(MaxMatriculasPorEstudiante), MaxMatriculasPorEstudiante, 0, 6);

        if (!PoliticaContrasena.Cumple(Contrasena))
        {
            errores.Add($"La opción '{SectionName}:{nameof(Contrasena)}' es obligatoria y debe cumplir RN-26: "
                + $"{PoliticaContrasena.Mensaje} Defínala en user-secrets.");
        }

        return errores;
    }

    private static void ValidarRango(List<string> errores, string clave, int valor, int minimo, int maximo)
    {
        if (valor < minimo || valor > maximo)
        {
            errores.Add($"La opción '{SectionName}:{clave}' debe estar entre {minimo} y {maximo}.");
        }
    }
}
