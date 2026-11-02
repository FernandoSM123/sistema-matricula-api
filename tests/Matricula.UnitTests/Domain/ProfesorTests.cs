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

    [Theory]
    [InlineData(Rol.Administrador)]
    [InlineData(Rol.Estudiante)]
    public void Constructor_CuentaDeOtroRol_LanzaArgumentException(Rol rol)
    {
        var usuario = new Usuario("otro@instituto.local", rol, OrigenRegistro.Administrador);

        Assert.Throws<ArgumentException>(() =>
            new Profesor(usuario, "1-1111-1111", "Ana", "Mora", "88888888", null, null));
    }

    [Fact]
    public void Constructor_DatosConEspacios_LosRecorta()
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EspecialidadVaciaOEnBlanco_QuedaNula(string? especialidad)
    {
        var profesor = new Profesor(CuentaProfesor(), "1-1111-1111", "Ana", "Mora", "88888888", especialidad, null);

        Assert.Null(profesor.Especialidad);
    }

    [Theory]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void Constructor_DatoObligatorioVacio_LanzaArgumentException(string cedula, string nombre, string apellidos, string telefono)
    {
        Assert.Throws<ArgumentException>(() =>
            new Profesor(CuentaProfesor(), cedula, nombre, apellidos, telefono, null, null));
    }

    [Fact]
    public void ActualizarDatos_CambiaTodosLosDatosPersonales()
    {
        var profesor = NuevoProfesor();
        var nuevaFecha = new DateOnly(2025, 8, 15);

        profesor.ActualizarDatos(" 2-2222-2222 ", " Ana Lucía ", " Mora Solís ", " 77777777 ", " Programación ", nuevaFecha);

        Assert.Equal("2-2222-2222", profesor.Cedula);
        Assert.Equal("Ana Lucía", profesor.Nombre);
        Assert.Equal("Mora Solís", profesor.Apellidos);
        Assert.Equal("77777777", profesor.Telefono);
        Assert.Equal("Programación", profesor.Especialidad);
        Assert.Equal(nuevaFecha, profesor.FechaContratacion);
    }

    [Fact]
    public void ActualizarDatos_EspecialidadEnBlancoYSinFecha_LasQuita()
    {
        var profesor = NuevoProfesor();

        profesor.ActualizarDatos("1-1111-1111", "Ana", "Mora", "88888888", " ", null);

        Assert.Null(profesor.Especialidad);
        Assert.Null(profesor.FechaContratacion);
    }

    [Theory]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void ActualizarDatos_DatoObligatorioVacio_LanzaArgumentExceptionYNoCambiaNada(
        string cedula, string nombre, string apellidos, string telefono)
    {
        var profesor = NuevoProfesor();

        Assert.Throws<ArgumentException>(() =>
            profesor.ActualizarDatos(cedula, nombre, apellidos, telefono, "Ofimática", null));

        Assert.Equal("1-1111-1111", profesor.Cedula);
        Assert.Equal("Ana", profesor.Nombre);
        Assert.Equal("Mora", profesor.Apellidos);
        Assert.Equal("88888888", profesor.Telefono);
        Assert.Equal("Redes", profesor.Especialidad);
        Assert.Equal(Contratacion, profesor.FechaContratacion);
    }
}
