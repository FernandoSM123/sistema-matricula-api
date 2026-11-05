using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;

namespace Matricula.UnitTests.Domain;

public class HorarioTests
{
    private static Horario CrearHorario(DiaSemana dia, int horaInicio, int horaFin) =>
        new(dia, new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));

    [Fact]
    public void Constructor_DatosValidos_AsignaLosDatos()
    {
        var horario = CrearHorario(DiaSemana.Miercoles, 18, 21);

        Assert.Equal(DiaSemana.Miercoles, horario.Dia);
        Assert.Equal(new TimeOnly(18, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(21, 0), horario.HoraFin);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 8)]
    public void Constructor_FinNoPosteriorAlInicio_LanzaRn15(int horaInicio, int horaFin)
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearHorario(DiaSemana.Lunes, horaInicio, horaFin));

        Assert.Equal("RN-15", excepcion.Code);
    }

    [Fact]
    public void SeTraslapaCon_TraslapeParcial_DevuelveTrue()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Lunes, 9, 11);

        Assert.True(a.SeTraslapaCon(b));
        Assert.True(b.SeTraslapaCon(a));
    }

    [Fact]
    public void SeTraslapaCon_BloquesContiguos_DevuelveFalse()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Lunes, 10, 12);

        Assert.False(a.SeTraslapaCon(b));
    }

    [Fact]
    public void SeTraslapaCon_DiasDistintos_DevuelveFalse()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Jueves, 8, 10);

        Assert.False(a.SeTraslapaCon(b));
    }
}
