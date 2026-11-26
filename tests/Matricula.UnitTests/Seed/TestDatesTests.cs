using Matricula.Domain.Entities;
using Matricula.Domain.Rules;
using Matricula.Infrastructure.Seed.TestData;

namespace Matricula.UnitTests.Seed;

public class TestDatesTests
{
    public static TheoryData<DateOnly> DaysOf2028()
    {
        var dias = new TheoryData<DateOnly>();
        for (var dia = new DateOnly(2028, 1, 1); dia.Year == 2028; dia = dia.AddDays(1))
        {
            dias.Add(dia);
        }

        return dias;
    }

    [Fact(DisplayName = "Los días de 2028 incluyen el 29 de febrero")]
    public void DaysOf2028_IncludesLeapDay()
    {
        Assert.Equal(366, DaysOf2028().Count);
    }

    [Theory(DisplayName = "Para cualquier día los periodos son coherentes, no se traslapan y tienen nombres distintos")]
    [MemberData(nameof(DaysOf2028))]
    public void Compute_AnyDay_ConsistentPeriodosWithoutOverlapsAndDistinctNombres(DateOnly hoy)
    {
        var periodos = TestDates.Compute(hoy);
        PeriodoSample[] todos = [periodos.Pasado, periodos.Vigente, periodos.Futuro];

        Assert.All(todos, p => Assert.True(
            PeriodoRules.AreConsistent(p.InicioClases, p.FinClases, p.InicioMatricula, p.FinMatricula, p.FechaLimiteRetiro),
            $"El periodo {p.Nombre} no cumple RN-16."));
        Assert.False(Overlap(periodos.Pasado, periodos.Vigente));
        Assert.False(Overlap(periodos.Vigente, periodos.Futuro));
        Assert.False(Overlap(periodos.Pasado, periodos.Futuro));
        Assert.Equal(3, todos.Select(p => p.Nombre).Distinct().Count());
    }

    [Theory(DisplayName = "Para cualquier día el periodo vigente permite matricular y retirar hoy")]
    [MemberData(nameof(DaysOf2028))]
    public void Compute_AnyDay_VigenteAllowsEnrollingAndWithdrawingToday(DateOnly hoy)
    {
        var vigente = Create(TestDates.Compute(hoy).Vigente);

        Assert.True(vigente.IsInMatriculaRange(hoy));
        Assert.True(vigente.AllowsRetiro(hoy));
    }

    [Fact(DisplayName = "El periodo pasado ya terminó y el futuro no ha empezado")]
    public void Compute_PastHasEndedAndFutureHasNotStarted()
    {
        var hoy = new DateOnly(2026, 11, 16);

        var periodos = TestDates.Compute(hoy);

        Assert.True(periodos.Pasado.FinClases < hoy);
        Assert.False(Create(periodos.Pasado).IsInMatriculaRange(hoy));
        Assert.True(periodos.Futuro.InicioMatricula > hoy);
    }

    [Theory(DisplayName = "El nombre del periodo es el año y el trimestre del inicio de clases, como 2027-T2")]
    [InlineData(2027, 5, 10, "2027-T2")]
    [InlineData(2027, 1, 1, "2027-T1")]
    [InlineData(2027, 3, 31, "2027-T1")]
    [InlineData(2027, 12, 31, "2027-T4")]
    public void PeriodoNombre_ClasesStart_YearAndQuarter(int anio, int mes, int dia, string esperado)
    {
        Assert.Equal(esperado, TestDates.PeriodoNombre(new DateOnly(anio, mes, dia)));
    }

    private static bool Overlap(PeriodoSample a, PeriodoSample b) =>
        PeriodoRules.ClasesOverlap(a.InicioClases, a.FinClases, b.InicioClases, b.FinClases);

    private static Periodo Create(PeriodoSample p) =>
        new(p.Nombre, p.InicioClases, p.FinClases, p.InicioMatricula, p.FinMatricula, p.FechaLimiteRetiro);
}
