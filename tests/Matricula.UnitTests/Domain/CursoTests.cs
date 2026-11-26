using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class CursoTests
{
    private static Curso CrearCurso(string codigo = "MAT-101", string? descripcion = "Fundamentos") =>
        new(codigo, "Matemática", descripcion, 40, 150000m, NivelCurso.Basico);

    [Fact(DisplayName = "Crear un curso con datos válidos lo deja activo")]
    public void Constructor_ValidData_CreatesActiveCurso()
    {
        var curso = CrearCurso();

        Assert.True(curso.Activo);
        Assert.Equal(40, curso.DuracionHoras);
        Assert.Equal(150000m, curso.Costo);
        Assert.Equal(NivelCurso.Basico, curso.Nivel);
    }

    [Fact(DisplayName = "Un código en minúsculas y con espacios se normaliza")]
    public void Constructor_LowercaseCodigoWithSpaces_NormalizesIt()
    {
        var curso = CrearCurso(codigo: "  mat-101 ");

        Assert.Equal("MAT-101", curso.Codigo);
    }

    [Fact(DisplayName = "Códigos con distinta capitalización se normalizan al mismo valor")]
    public void NormalizeCodigo_DifferentCasing_ReturnsSameCodigo()
    {
        Assert.Equal(Curso.NormalizeCodigo("MAT-101"), Curso.NormalizeCodigo(" mat-101 "));
    }

    [Theory(DisplayName = "Un código en blanco lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCodigo_BlankCodigo_Throws(string codigo)
    {
        Assert.Throws<ArgumentException>(() => Curso.NormalizeCodigo(codigo));
    }

    [Fact(DisplayName = "Los textos con espacios se recortan")]
    public void Constructor_TextsWithSpaces_TrimsThem()
    {
        var curso = new Curso("MAT-101", "  Matemática ", "  Fundamentos ", 40, 0m, NivelCurso.Basico);

        Assert.Equal("Matemática", curso.Nombre);
        Assert.Equal("Fundamentos", curso.Descripcion);
    }

    [Theory(DisplayName = "Una descripción en blanco queda nula")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankDescripcion_StaysNull(string? descripcion)
    {
        var curso = CrearCurso(descripcion: descripcion);

        Assert.Null(curso.Descripcion);
    }

    [Fact(DisplayName = "Actualizar con datos nuevos cambia todos los datos")]
    public void Update_NewData_ChangesAllData()
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

    [Fact(DisplayName = "Actualizar un curso inactivo conserva su estado")]
    public void Update_InactiveCurso_KeepsEstado()
    {
        var curso = CrearCurso();
        curso.Deactivate();

        curso.Update("MAT-101", "Matemática", null, 40, 0m, NivelCurso.Basico);

        Assert.False(curso.Activo);
    }

    [Fact(DisplayName = "Desactivar un curso activo lo deja inactivo")]
    public void Deactivate_ActiveCurso_MakesItInactive()
    {
        var curso = CrearCurso();

        curso.Deactivate();

        Assert.False(curso.Activo);
    }

    [Fact(DisplayName = "Reactivar un curso inactivo lo deja activo")]
    public void Reactivate_InactiveCurso_MakesItActive()
    {
        var curso = CrearCurso();
        curso.Deactivate();

        curso.Reactivate();

        Assert.True(curso.Activo);
    }
}
