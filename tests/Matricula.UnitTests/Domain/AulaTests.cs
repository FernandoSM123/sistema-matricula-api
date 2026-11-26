using Matricula.Domain.Entities;

namespace Matricula.UnitTests.Domain;

public class AulaTests
{
    private static Aula CrearAula(string codigo = "LAB-1", string? ubicacion = "Edificio A") =>
        new(codigo, ubicacion, 25);

    [Fact(DisplayName = "Crear un aula con datos válidos la deja activa")]
    public void Constructor_ValidData_CreatesActiveAula()
    {
        var aula = CrearAula();

        Assert.True(aula.Activo);
        Assert.Equal(25, aula.Capacidad);
    }

    [Fact(DisplayName = "Un código en minúsculas y con espacios se normaliza")]
    public void Constructor_LowercaseCodigoWithSpaces_NormalizesIt()
    {
        var aula = CrearAula(codigo: "  lab-1 ");

        Assert.Equal("LAB-1", aula.Codigo);
    }

    [Fact(DisplayName = "Códigos con distinta capitalización se normalizan al mismo valor")]
    public void NormalizeCodigo_DifferentCasing_ReturnsSameCodigo()
    {
        Assert.Equal(Aula.NormalizeCodigo("LAB-1"), Aula.NormalizeCodigo(" lab-1 "));
    }

    [Theory(DisplayName = "Un código en blanco lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCodigo_BlankCodigo_Throws(string codigo)
    {
        Assert.Throws<ArgumentException>(() => Aula.NormalizeCodigo(codigo));
    }

    [Fact(DisplayName = "La ubicación con espacios se recorta")]
    public void Constructor_UbicacionWithSpaces_TrimsIt()
    {
        var aula = CrearAula(ubicacion: "  Edificio A ");

        Assert.Equal("Edificio A", aula.Ubicacion);
    }

    [Theory(DisplayName = "Una ubicación en blanco queda nula")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankUbicacion_StaysNull(string? ubicacion)
    {
        var aula = CrearAula(ubicacion: ubicacion);

        Assert.Null(aula.Ubicacion);
    }

    [Fact(DisplayName = "Actualizar con datos nuevos cambia todos los datos")]
    public void Update_NewData_ChangesAllData()
    {
        var aula = CrearAula();

        aula.Update("aud-2", "Edificio B", 80);

        Assert.Equal("AUD-2", aula.Codigo);
        Assert.Equal("Edificio B", aula.Ubicacion);
        Assert.Equal(80, aula.Capacidad);
    }

    [Fact(DisplayName = "Actualizar un aula inactiva conserva su estado")]
    public void Update_InactiveAula_KeepsEstado()
    {
        var aula = CrearAula();
        aula.Deactivate();

        aula.Update("LAB-1", null, 25);

        Assert.False(aula.Activo);
    }

    [Fact(DisplayName = "Desactivar un aula activa la deja inactiva")]
    public void Deactivate_ActiveAula_MakesItInactive()
    {
        var aula = CrearAula();

        aula.Deactivate();

        Assert.False(aula.Activo);
    }

    [Fact(DisplayName = "Reactivar un aula inactiva la deja activa")]
    public void Reactivate_InactiveAula_MakesItActive()
    {
        var aula = CrearAula();
        aula.Deactivate();

        aula.Reactivate();

        Assert.True(aula.Activo);
    }
}
