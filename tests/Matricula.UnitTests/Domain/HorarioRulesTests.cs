using System.Globalization;
using Matricula.Domain.Enums;
using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class HorarioRulesTests
{
    private static readonly TimeOnly Ocho = new(8, 0);
    private static readonly TimeOnly Diez = new(10, 0);

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void AreValidHours_SegunMinutosRespectoAlInicio_DevuelveLoEsperado(int minutos, bool esperado)
    {
        Assert.Equal(esperado, HorarioRules.AreValidHours(Ocho, Ocho.AddMinutes(minutos)));
    }

    [Theory]
    [InlineData("09:00", "11:00", true)]  // traslape parcial por el final
    [InlineData("07:00", "09:00", true)]  // traslape parcial por el inicio
    [InlineData("08:30", "09:30", true)]  // contenido
    [InlineData("07:00", "11:00", true)]  // lo contiene
    [InlineData("08:00", "10:00", true)]  // idéntico
    [InlineData("10:00", "12:00", false)] // contiguo después
    [InlineData("06:00", "08:00", false)] // contiguo antes
    [InlineData("11:00", "12:00", false)] // separado
    public void Overlap_MismoDia_DevuelveLoEsperado(string inicio, string fin, bool esperado)
    {
        var resultado = HorarioRules.Overlap(
            DiaSemana.Lunes,
            Ocho,
            Diez,
            DiaSemana.Lunes,
            TimeOnly.Parse(inicio, CultureInfo.InvariantCulture),
            TimeOnly.Parse(fin, CultureInfo.InvariantCulture));

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Overlap_MismasHorasEnDiasDistintos_DevuelveFalse()
    {
        Assert.False(HorarioRules.Overlap(DiaSemana.Lunes, Ocho, Diez, DiaSemana.Martes, Ocho, Diez));
    }

    [Fact]
    public void Overlap_OrdenDeLosBloques_NoCambiaElResultado()
    {
        var nueve = new TimeOnly(9, 0);
        var once = new TimeOnly(11, 0);

        Assert.Equal(
            HorarioRules.Overlap(DiaSemana.Lunes, Ocho, Diez, DiaSemana.Lunes, nueve, once),
            HorarioRules.Overlap(DiaSemana.Lunes, nueve, once, DiaSemana.Lunes, Ocho, Diez));
    }
}
