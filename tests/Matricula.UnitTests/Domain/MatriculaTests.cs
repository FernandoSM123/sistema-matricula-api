using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
// "Matricula" a secas resuelve al espacio de nombres raíz, no a la entidad.
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.UnitTests.Domain;

public class MatriculaTests
{
    private static readonly DateOnly Hoy = new(2027, 2, 15);

    [Fact]
    public void Constructor_DatosValidos_CreaActivaSinRetiro()
    {
        var matricula = new MatriculaEntidad(estudianteId: 7, grupoId: 9, Hoy);

        Assert.Equal(7, matricula.EstudianteId);
        Assert.Equal(9, matricula.GrupoId);
        Assert.Equal(Hoy, matricula.FechaMatricula);
        Assert.Equal(EstadoMatricula.Activa, matricula.Estado);
        Assert.Null(matricula.FechaRetiro);
        Assert.Null(matricula.MotivoRetiro);
    }

    [Theory]
    [InlineData(MotivoRetiro.RetiroVoluntario)]
    [InlineData(MotivoRetiro.CambioGrupo)]
    [InlineData(MotivoRetiro.CancelacionGrupo)]
    public void Retirar_MatriculaActiva_AsignaEstadoFechaYMotivo(MotivoRetiro motivo)
    {
        var matricula = new MatriculaEntidad(7, 9, Hoy);
        var fecha = Hoy.AddDays(10);

        matricula.Retirar(fecha, motivo);

        Assert.Equal(EstadoMatricula.Retirada, matricula.Estado);
        Assert.Equal(fecha, matricula.FechaRetiro);
        Assert.Equal(motivo, matricula.MotivoRetiro);
        Assert.Equal(Hoy, matricula.FechaMatricula);
    }

    [Fact]
    public void Retirar_MatriculaYaRetirada_LanzaRf52YConservaElPrimerRetiro()
    {
        var matricula = new MatriculaEntidad(7, 9, Hoy);
        matricula.Retirar(Hoy.AddDays(1), MotivoRetiro.RetiroVoluntario);

        var excepcion = Assert.Throws<BusinessRuleException>(
            () => matricula.Retirar(Hoy.AddDays(2), MotivoRetiro.CancelacionGrupo));

        Assert.Equal("RF-52", excepcion.Code);
        Assert.Equal(Hoy.AddDays(1), matricula.FechaRetiro);
        Assert.Equal(MotivoRetiro.RetiroVoluntario, matricula.MotivoRetiro);
    }
}
