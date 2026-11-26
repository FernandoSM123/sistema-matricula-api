using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;

namespace Matricula.UnitTests.Domain;

public class HorarioTests
{
    private static Horario CrearHorario(DiaSemana dia, int horaInicio, int horaFin) =>
        new(dia, new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));

    [Fact(DisplayName = "Crear un bloque con datos válidos asigna sus datos")]
    public void Constructor_ValidData_AssignsData()
    {
        var horario = CrearHorario(DiaSemana.Miercoles, 18, 21);

        Assert.Equal(DiaSemana.Miercoles, horario.Dia);
        Assert.Equal(new TimeOnly(18, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(21, 0), horario.HoraFin);
    }

    [Theory(DisplayName = "Un fin que no es posterior al inicio lanza RN-15")]
    [InlineData(10, 10)]
    [InlineData(10, 8)]
    public void Constructor_EndNotAfterStart_ThrowsRn15(int horaInicio, int horaFin)
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearHorario(DiaSemana.Lunes, horaInicio, horaFin));

        Assert.Equal("RN-15", excepcion.Code);
    }

    [Fact(DisplayName = "Un traslape parcial se detecta (RN-14)")]
    public void OverlapsWith_PartialOverlap_ReturnsTrue()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Lunes, 9, 11);

        Assert.True(a.OverlapsWith(b));
        Assert.True(b.OverlapsWith(a));
    }

    [Fact(DisplayName = "Dos bloques contiguos no se traslapan")]
    public void OverlapsWith_ContiguousBlocks_ReturnsFalse()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Lunes, 10, 12);

        Assert.False(a.OverlapsWith(b));
    }

    [Fact(DisplayName = "Dos bloques de días distintos no se traslapan")]
    public void OverlapsWith_DifferentDays_ReturnsFalse()
    {
        var a = CrearHorario(DiaSemana.Lunes, 8, 10);
        var b = CrearHorario(DiaSemana.Jueves, 8, 10);

        Assert.False(a.OverlapsWith(b));
    }
}
