namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Fechas de los periodos relativas a hoy (punto 5 de la spec), para que los datos sigan siendo útiles con el tiempo.
/// Todas cumplen RN-16 y las clases de los tres no se traslapan (RN-06 entre periodos).
/// </summary>
public static class TestDates
{
    public static PeriodoSamples Compute(DateOnly hoy) => new(
        Pasado: Create(hoy, inicioMatricula: -200, finMatricula: -175, limiteRetiro: -150, inicioClases: -180, finClases: -60),
        Vigente: Create(hoy, inicioMatricula: -10, finMatricula: 10, limiteRetiro: 30, inicioClases: 15, finClases: 120),
        Futuro: Create(hoy, inicioMatricula: 130, finMatricula: 145, limiteRetiro: 170, inicioClases: 150, finClases: 260));

    /// <summary>
    /// <c>&lt;año&gt;-T&lt;trimestre&gt;</c> del inicio de clases. Los inicios de los tres periodos están separados por
    /// 135 días o más, así que nunca caen en el mismo trimestre.
    /// </summary>
    public static string PeriodoNombre(DateOnly inicioClases) => $"{inicioClases.Year}-T{((inicioClases.Month - 1) / 3) + 1}";

    private static PeriodoSample Create(
        DateOnly hoy, int inicioMatricula, int finMatricula, int limiteRetiro, int inicioClases, int finClases)
    {
        var inicio = hoy.AddDays(inicioClases);
        return new PeriodoSample(
            PeriodoNombre(inicio),
            inicio,
            hoy.AddDays(finClases),
            hoy.AddDays(inicioMatricula),
            hoy.AddDays(finMatricula),
            hoy.AddDays(limiteRetiro));
    }
}
