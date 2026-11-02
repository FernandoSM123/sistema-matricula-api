using Matricula.Domain.Entities;

namespace Matricula.UnitTests.Domain;

public class AulaTests
{
    private static Aula CrearAula(string codigo = "LAB-1", string? ubicacion = "Edificio A") =>
        new(codigo, ubicacion, 25);

    [Fact]
    public void Constructor_DatosValidos_CreaElAulaActiva()
    {
        var aula = CrearAula();

        Assert.True(aula.Activo);
        Assert.Equal(25, aula.Capacidad);
    }

    [Fact]
    public void Constructor_CodigoEnMinusculasConEspacios_LoNormaliza()
    {
        var aula = CrearAula(codigo: "  lab-1 ");

        Assert.Equal("LAB-1", aula.Codigo);
    }

    [Fact]
    public void NormalizarCodigo_CapitalizacionDistinta_DevuelveElMismoCodigo()
    {
        Assert.Equal(Aula.NormalizarCodigo("LAB-1"), Aula.NormalizarCodigo(" lab-1 "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarCodigo_CodigoEnBlanco_LanzaExcepcion(string codigo)
    {
        Assert.Throws<ArgumentException>(() => Aula.NormalizarCodigo(codigo));
    }

    [Fact]
    public void Constructor_UbicacionConEspacios_LaRecorta()
    {
        var aula = CrearAula(ubicacion: "  Edificio A ");

        Assert.Equal("Edificio A", aula.Ubicacion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_UbicacionEnBlanco_QuedaNull(string? ubicacion)
    {
        var aula = CrearAula(ubicacion: ubicacion);

        Assert.Null(aula.Ubicacion);
    }

    [Fact]
    public void ActualizarDatos_DatosNuevos_CambiaTodosLosDatos()
    {
        var aula = CrearAula();

        aula.ActualizarDatos("aud-2", "Edificio B", 80);

        Assert.Equal("AUD-2", aula.Codigo);
        Assert.Equal("Edificio B", aula.Ubicacion);
        Assert.Equal(80, aula.Capacidad);
    }

    [Fact]
    public void ActualizarDatos_AulaInactiva_ConservaElEstado()
    {
        var aula = CrearAula();
        aula.Desactivar();

        aula.ActualizarDatos("LAB-1", null, 25);

        Assert.False(aula.Activo);
    }

    [Fact]
    public void Desactivar_AulaActiva_LaDejaInactiva()
    {
        var aula = CrearAula();

        aula.Desactivar();

        Assert.False(aula.Activo);
    }

    [Fact]
    public void Reactivar_AulaInactiva_LaDejaActiva()
    {
        var aula = CrearAula();
        aula.Desactivar();

        aula.Reactivar();

        Assert.True(aula.Activo);
    }
}
