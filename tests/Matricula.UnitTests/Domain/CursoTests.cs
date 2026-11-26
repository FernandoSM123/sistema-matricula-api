using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class CursoTests
{
    private static Curso CrearCurso(string codigo = "MAT-101", string? descripcion = "Fundamentos") =>
        new(codigo, "Matemática", descripcion, 40, 150000m, NivelCurso.Basico);

    [Fact]
    public void Constructor_DatosValidos_CreaElCursoActivo()
    {
        var curso = CrearCurso();

        Assert.True(curso.Activo);
        Assert.Equal(40, curso.DuracionHoras);
        Assert.Equal(150000m, curso.Costo);
        Assert.Equal(NivelCurso.Basico, curso.Nivel);
    }

    [Fact]
    public void Constructor_CodigoEnMinusculasConEspacios_LoNormaliza()
    {
        var curso = CrearCurso(codigo: "  mat-101 ");

        Assert.Equal("MAT-101", curso.Codigo);
    }

    [Fact]
    public void NormalizeCodigo_CapitalizacionDistinta_DevuelveElMismoCodigo()
    {
        Assert.Equal(Curso.NormalizeCodigo("MAT-101"), Curso.NormalizeCodigo(" mat-101 "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCodigo_CodigoEnBlanco_LanzaExcepcion(string codigo)
    {
        Assert.Throws<ArgumentException>(() => Curso.NormalizeCodigo(codigo));
    }

    [Fact]
    public void Constructor_TextosConEspacios_LosRecorta()
    {
        var curso = new Curso("MAT-101", "  Matemática ", "  Fundamentos ", 40, 0m, NivelCurso.Basico);

        Assert.Equal("Matemática", curso.Nombre);
        Assert.Equal("Fundamentos", curso.Descripcion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_DescripcionEnBlanco_QuedaNull(string? descripcion)
    {
        var curso = CrearCurso(descripcion: descripcion);

        Assert.Null(curso.Descripcion);
    }

    [Fact]
    public void Update_DatosNuevos_CambiaTodosLosDatos()
    {
        var curso = CrearCurso();

        curso.Update("fis-200", "Física", "Mecánica", 60, 200000.50m, NivelCurso.Avanzado);

        Assert.Equal("FIS-200", curso.Codigo);
        Assert.Equal("Física", curso.Nombre);
        Assert.Equal("Mecánica", curso.Descripcion);
        Assert.Equal(60, curso.DuracionHoras);
        Assert.Equal(200000.50m, curso.Costo);
        Assert.Equal(NivelCurso.Avanzado, curso.Nivel);
    }

    [Fact]
    public void Update_CursoInactivo_ConservaElEstado()
    {
        var curso = CrearCurso();
        curso.Deactivate();

        curso.Update("MAT-101", "Matemática", null, 40, 0m, NivelCurso.Basico);

        Assert.False(curso.Activo);
    }

    [Fact]
    public void Deactivate_CursoActivo_LoDejaInactivo()
    {
        var curso = CrearCurso();

        curso.Deactivate();

        Assert.False(curso.Activo);
    }

    [Fact]
    public void Reactivate_CursoInactivo_LoDejaActivo()
    {
        var curso = CrearCurso();
        curso.Deactivate();

        curso.Reactivate();

        Assert.True(curso.Activo);
    }
}
