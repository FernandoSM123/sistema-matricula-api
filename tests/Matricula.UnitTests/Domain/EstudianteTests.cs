using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class EstudianteTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 27);
    private static readonly DateOnly Nacimiento = new(2000, 5, 14);

    private static Usuario CuentaEstudiante() =>
        new("ana@instituto.local", Rol.Estudiante, OrigenRegistro.RegistroPublico);

    private static Estudiante NuevoEstudiante() =>
        new(CuentaEstudiante(), "1-1111-1111", "Ana", "Mora", "88888888", Nacimiento, "San José", Hoy);

    [Theory]
    [InlineData(Rol.Administrador)]
    [InlineData(Rol.Profesor)]
    public void Constructor_CuentaDeOtroRol_LanzaArgumentException(Rol rol)
    {
        var usuario = new Usuario("otro@instituto.local", rol, OrigenRegistro.Administrador);

        Assert.Throws<ArgumentException>(() =>
            new Estudiante(usuario, "1-1111-1111", "Ana", "Mora", "88888888", null, null, Hoy));
    }

    [Fact]
    public void Constructor_DatosConEspacios_LosRecorta()
    {
        var estudiante = new Estudiante(
            CuentaEstudiante(), " 1-1111-1111 ", "  Ana ", " Mora Rojas ", " 8888 8888 ", Nacimiento, "  San José  ", Hoy);

        Assert.Equal("1-1111-1111", estudiante.Cedula);
        Assert.Equal("Ana", estudiante.Nombre);
        Assert.Equal("Mora Rojas", estudiante.Apellidos);
        Assert.Equal("8888 8888", estudiante.Telefono);
        Assert.Equal("San José", estudiante.Direccion);
        Assert.Equal(Nacimiento, estudiante.FechaNacimiento);
        Assert.Equal(Hoy, estudiante.FechaRegistro);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_DireccionVaciaOEnBlanco_QuedaNula(string? direccion)
    {
        var estudiante = new Estudiante(CuentaEstudiante(), "1-1111-1111", "Ana", "Mora", "88888888", null, direccion, Hoy);

        Assert.Null(estudiante.Direccion);
    }

    [Theory]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void Constructor_DatoObligatorioVacio_LanzaArgumentException(string cedula, string nombre, string apellidos, string telefono)
    {
        Assert.Throws<ArgumentException>(() =>
            new Estudiante(CuentaEstudiante(), cedula, nombre, apellidos, telefono, null, null, Hoy));
    }

    [Fact]
    public void ActualizarContacto_SoloCambiaTelefonoYDireccion()
    {
        var estudiante = NuevoEstudiante();

        estudiante.ActualizarContacto(" 7777-7777 ", " Heredia ");

        Assert.Equal("7777-7777", estudiante.Telefono);
        Assert.Equal("Heredia", estudiante.Direccion);
        Assert.Equal("1-1111-1111", estudiante.Cedula);
        Assert.Equal("Ana", estudiante.Nombre);
        Assert.Equal("Mora", estudiante.Apellidos);
        Assert.Equal(Nacimiento, estudiante.FechaNacimiento);
    }

    [Fact]
    public void ActualizarContacto_DireccionEnBlanco_LaQuita()
    {
        var estudiante = NuevoEstudiante();

        estudiante.ActualizarContacto("88888888", " ");

        Assert.Null(estudiante.Direccion);
    }

    [Fact]
    public void ActualizarDatos_CambiaLosDatosPersonalesYConservaLaFechaDeRegistro()
    {
        var estudiante = NuevoEstudiante();
        var nuevaFecha = new DateOnly(2001, 1, 2);

        estudiante.ActualizarDatos("2-2222-2222", "Ana Lucía", "Mora Solís", "77777777", nuevaFecha, null);

        Assert.Equal("2-2222-2222", estudiante.Cedula);
        Assert.Equal("Ana Lucía", estudiante.Nombre);
        Assert.Equal("Mora Solís", estudiante.Apellidos);
        Assert.Equal("77777777", estudiante.Telefono);
        Assert.Equal(nuevaFecha, estudiante.FechaNacimiento);
        Assert.Null(estudiante.Direccion);
        Assert.Equal(Hoy, estudiante.FechaRegistro);
    }
}
