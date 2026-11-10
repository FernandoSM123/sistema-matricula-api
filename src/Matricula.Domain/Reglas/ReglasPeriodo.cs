namespace Matricula.Domain.Reglas;

/// <summary>
/// RN-16: coherencia de las fechas de un periodo. "Dentro del periodo" incluye la etapa de matrícula, para que un
/// estudiante pueda retirarse antes de que empiecen las clases. La matrícula puede terminar después del inicio de
/// clases, para permitir la matrícula tardía.
/// </summary>
public static class ReglasPeriodo
{
    public const string MensajeMatriculaOrdenada = "El inicio de la matrícula debe ser anterior a su fin.";

    public const string MensajeClasesOrdenadas = "El inicio de clases debe ser anterior al fin de clases.";

    public const string MensajeMatriculaDentroDeClases = "La matrícula no puede terminar después del fin de clases.";

    public const string MensajeRetiroDentroDelPeriodo =
        "La fecha límite de retiro debe estar entre el inicio de la matrícula y el fin de clases.";

    public static bool MatriculaOrdenada(DateOnly inicioMatricula, DateOnly finMatricula) =>
        inicioMatricula < finMatricula;

    public static bool ClasesOrdenadas(DateOnly inicioClases, DateOnly finClases) =>
        inicioClases < finClases;

    public static bool MatriculaDentroDeClases(DateOnly finMatricula, DateOnly finClases) =>
        finMatricula <= finClases;

    public static bool RetiroDentroDelPeriodo(DateOnly fechaLimiteRetiro, DateOnly inicioMatricula, DateOnly finClases) =>
        inicioMatricula <= fechaLimiteRetiro && fechaLimiteRetiro <= finClases;

    /// <summary>
    /// Supuesto 4 de la spec de matriculas (RN-06): dos periodos se traslapan si sus clases comparten al menos un día,
    /// ambos extremos incluidos.
    /// </summary>
    public static bool ClasesSeTraslapan(DateOnly inicioA, DateOnly finA, DateOnly inicioB, DateOnly finB) =>
        inicioA <= finB && inicioB <= finA;

    public static bool Coherente(
        DateOnly inicioClases,
        DateOnly finClases,
        DateOnly inicioMatricula,
        DateOnly finMatricula,
        DateOnly fechaLimiteRetiro) =>
        MatriculaOrdenada(inicioMatricula, finMatricula)
        && ClasesOrdenadas(inicioClases, finClases)
        && MatriculaDentroDeClases(finMatricula, finClases)
        && RetiroDentroDelPeriodo(fechaLimiteRetiro, inicioMatricula, finClases);
}
