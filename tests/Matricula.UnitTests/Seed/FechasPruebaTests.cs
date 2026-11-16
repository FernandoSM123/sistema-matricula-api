using Matricula.Domain.Entities;
using Matricula.Domain.Reglas;
using Matricula.Infrastructure.Seed.DatosPrueba;

namespace Matricula.UnitTests.Seed;

public class FechasPruebaTests
{
    public static TheoryData<DateOnly> DiasDe2028()
    {
        var dias = new TheoryData<DateOnly>();
        for (var dia = new DateOnly(2028, 1, 1); dia.Year == 2028; dia = dia.AddDays(1))
        {
            dias.Add(dia);
        }

        return dias;
    }

    [Fact]
    public void DiasDe2028_IncluyeElDiaBisiesto()
    {
        Assert.Equal(366, DiasDe2028().Count);
    }

    [Theory]
    [MemberData(nameof(DiasDe2028))]
    public void Calcular_CualquierDia_PeriodosCoherentesSinTraslapesYConNombresDistintos(DateOnly hoy)
    {
        var periodos = FechasPrueba.Calcular(hoy);
        PeriodoPrueba[] todos = [periodos.Pasado, periodos.Vigente, periodos.Futuro];

        Assert.All(todos, p => Assert.True(
            ReglasPeriodo.Coherente(p.InicioClases, p.FinClases, p.InicioMatricula, p.FinMatricula, p.FechaLimiteRetiro),
            $"El periodo {p.Nombre} no cumple RN-16."));
        Assert.False(SeTraslapan(periodos.Pasado, periodos.Vigente));
        Assert.False(SeTraslapan(periodos.Vigente, periodos.Futuro));
        Assert.False(SeTraslapan(periodos.Pasado, periodos.Futuro));
        Assert.Equal(3, todos.Select(p => p.Nombre).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(DiasDe2028))]
    public void Calcular_CualquierDia_ElVigentePermiteMatricularYRetirarHoy(DateOnly hoy)
    {
        var vigente = Crear(FechasPrueba.Calcular(hoy).Vigente);

        Assert.True(vigente.EnRangoMatricula(hoy));
        Assert.True(vigente.PermiteRetiro(hoy));
    }

    [Fact]
    public void Calcular_ElPasadoYaTerminoYElFuturoNoHaEmpezado()
    {
        var hoy = new DateOnly(2026, 11, 16);

        var periodos = FechasPrueba.Calcular(hoy);

        Assert.True(periodos.Pasado.FinClases < hoy);
        Assert.False(Crear(periodos.Pasado).EnRangoMatricula(hoy));
        Assert.True(periodos.Futuro.InicioMatricula > hoy);
    }

    [Theory]
    [InlineData(2027, 5, 10, "2027-T2")]
    [InlineData(2027, 1, 1, "2027-T1")]
    [InlineData(2027, 3, 31, "2027-T1")]
    [InlineData(2027, 12, 31, "2027-T4")]
    public void Nombre_InicioDeClases_AnioYTrimestre(int anio, int mes, int dia, string esperado)
    {
        Assert.Equal(esperado, FechasPrueba.Nombre(new DateOnly(anio, mes, dia)));
    }

    private static bool SeTraslapan(PeriodoPrueba a, PeriodoPrueba b) =>
        ReglasPeriodo.ClasesSeTraslapan(a.InicioClases, a.FinClases, b.InicioClases, b.FinClases);

    private static Periodo Crear(PeriodoPrueba p) =>
        new(p.Nombre, p.InicioClases, p.FinClases, p.InicioMatricula, p.FinMatricula, p.FechaLimiteRetiro);
}
