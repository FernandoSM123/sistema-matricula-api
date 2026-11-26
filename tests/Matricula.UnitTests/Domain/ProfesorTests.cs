using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class ProfesorTests
{
    private static readonly DateOnly Contratacion = new(2024, 2, 1);

    private static Usuario CuentaProfesor() =>
        new("ana@instituto.local", Rol.Profesor, OrigenRegistro.Administrador);

    private static Profesor NuevoProfesor() =>
        new(CuentaProfesor(), "1-1111-1111", "Ana", "Mora", "88888888", "Redes", Contratacion);

    [Theory(DisplayName = "Crear un profesor con una cuenta de otro rol lanza ArgumentException")]
    [InlineData(Rol.Administrador)]
    [InlineData(Rol.Estudiante)]
    public void Constructor_AccountWithOtherRol_ThrowsArgumentException(Rol rol)
    {
        var usuario = new Usuario("otro@instituto.local", rol, OrigenRegistro.Administrador);

        Assert.Throws<ArgumentException>(() =>
            new Profesor(usuario, "1-1111-1111", "Ana", "Mora", "88888888", null, null));
    }

    [Fact(DisplayName = "Los datos con espacios se recortan")]
    public void Constructor_DataWithSpaces_TrimsThem()
    {
        var profesor = new Profesor(
            CuentaProfesor(), " 1-1111-1111 ", "  Ana ", " Mora Rojas ", " 8888 8888 ", "  Redes  ", Contratacion);

        Assert.Equal("1-1111-1111", profesor.Cedula);
        Assert.Equal("Ana", profesor.Nombre);
        Assert.Equal("Mora Rojas", profesor.Apellidos);
        Assert.Equal("8888 8888", profesor.Telefono);
        Assert.Equal("Redes", profesor.Especialidad);
        Assert.Equal(Contratacion, profesor.FechaContratacion);
    }

    [Theory(DisplayName = "Una especialidad vacía o en blanco queda nula")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrBlankEspecialidad_StaysNull(string? especialidad)
    {
        var profesor = new Profesor(CuentaProfesor(), "1-1111-1111", "Ana", "Mora", "88888888", especialidad, null);

        Assert.Null(profesor.Especialidad);
    }

    [Theory(DisplayName = "Un dato obligatorio vacío lanza ArgumentException")]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void Constructor_EmptyRequiredData_ThrowsArgumentException(string cedula, string nombre, string apellidos, string telefono)
    {
        Assert.Throws<ArgumentException>(() =>
            new Profesor(CuentaProfesor(), cedula, nombre, apellidos, telefono, null, null));
    }

    [Fact(DisplayName = "Actualizar cambia todos los datos personales")]
    public void Update_ChangesAllPersonalData()
    {
        var profesor = NuevoProfesor();
        var nuevaFecha = new DateOnly(2025, 8, 15);

        profesor.Update(" 2-2222-2222 ", " Ana Lucía ", " Mora Solís ", " 77777777 ", " Programación ", nuevaFecha);

        Assert.Equal("2-2222-2222", profesor.Cedula);
        Assert.Equal("Ana Lucía", profesor.Nombre);
        Assert.Equal("Mora Solís", profesor.Apellidos);
        Assert.Equal("77777777", profesor.Telefono);
        Assert.Equal("Programación", profesor.Especialidad);
        Assert.Equal(nuevaFecha, profesor.FechaContratacion);
    }

    [Fact(DisplayName = "Una especialidad en blanco y sin fecha de contratación las quita")]
    public void Update_BlankEspecialidadAndNoDate_RemovesThem()
    {
        var profesor = NuevoProfesor();

        profesor.Update("1-1111-1111", "Ana", "Mora", "88888888", " ", null);

        Assert.Null(profesor.Especialidad);
        Assert.Null(profesor.FechaContratacion);
    }

    [Theory(DisplayName = "Un dato obligatorio vacío lanza ArgumentException y no cambia nada")]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void Update_EmptyRequiredData_ThrowsArgumentExceptionAndChangesNothing(
        string cedula, string nombre, string apellidos, string telefono)
    {
        var profesor = NuevoProfesor();

        Assert.Throws<ArgumentException>(() =>
            profesor.Update(cedula, nombre, apellidos, telefono, "Ofimática", null));

        Assert.Equal("1-1111-1111", profesor.Cedula);
        Assert.Equal("Ana", profesor.Nombre);
        Assert.Equal("Mora", profesor.Apellidos);
        Assert.Equal("88888888", profesor.Telefono);
        Assert.Equal("Redes", profesor.Especialidad);
        Assert.Equal(Contratacion, profesor.FechaContratacion);
    }
}
