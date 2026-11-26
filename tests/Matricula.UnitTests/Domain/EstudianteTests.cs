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

    [Theory(DisplayName = "Crear un estudiante con una cuenta de otro rol lanza ArgumentException")]
    [InlineData(Rol.Administrador)]
    [InlineData(Rol.Profesor)]
    public void Constructor_AccountWithOtherRol_ThrowsArgumentException(Rol rol)
    {
        var usuario = new Usuario("otro@instituto.local", rol, OrigenRegistro.Administrador);

        Assert.Throws<ArgumentException>(() =>
            new Estudiante(usuario, "1-1111-1111", "Ana", "Mora", "88888888", null, null, Hoy));
    }

    [Fact(DisplayName = "Los datos con espacios se recortan")]
    public void Constructor_DataWithSpaces_TrimsThem()
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

    [Theory(DisplayName = "Una dirección vacía o en blanco queda nula")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrBlankDireccion_StaysNull(string? direccion)
    {
        var estudiante = new Estudiante(CuentaEstudiante(), "1-1111-1111", "Ana", "Mora", "88888888", null, direccion, Hoy);

        Assert.Null(estudiante.Direccion);
    }

    [Theory(DisplayName = "Un dato obligatorio vacío lanza ArgumentException")]
    [InlineData("", "Ana", "Mora", "88888888")]
    [InlineData("1-1111-1111", " ", "Mora", "88888888")]
    [InlineData("1-1111-1111", "Ana", "", "88888888")]
    [InlineData("1-1111-1111", "Ana", "Mora", "  ")]
    public void Constructor_EmptyRequiredData_ThrowsArgumentException(string cedula, string nombre, string apellidos, string telefono)
    {
        Assert.Throws<ArgumentException>(() =>
            new Estudiante(CuentaEstudiante(), cedula, nombre, apellidos, telefono, null, null, Hoy));
    }

    [Fact(DisplayName = "Actualizar el contacto solo cambia el teléfono y la dirección")]
    public void UpdateContacto_ChangesOnlyTelefonoAndDireccion()
    {
        var estudiante = NuevoEstudiante();

        estudiante.UpdateContacto(" 7777-7777 ", " Heredia ");

        Assert.Equal("7777-7777", estudiante.Telefono);
        Assert.Equal("Heredia", estudiante.Direccion);
        Assert.Equal("1-1111-1111", estudiante.Cedula);
        Assert.Equal("Ana", estudiante.Nombre);
        Assert.Equal("Mora", estudiante.Apellidos);
        Assert.Equal(Nacimiento, estudiante.FechaNacimiento);
    }

    [Fact(DisplayName = "Una dirección en blanco al actualizar el contacto la quita")]
    public void UpdateContacto_BlankDireccion_RemovesIt()
    {
        var estudiante = NuevoEstudiante();

        estudiante.UpdateContacto("88888888", " ");

        Assert.Null(estudiante.Direccion);
    }

    [Fact(DisplayName = "Actualizar cambia los datos personales y conserva la fecha de registro")]
    public void Update_ChangesPersonalDataAndKeepsFechaRegistro()
    {
        var estudiante = NuevoEstudiante();
        var nuevaFecha = new DateOnly(2001, 1, 2);

        estudiante.Update("2-2222-2222", "Ana Lucía", "Mora Solís", "77777777", nuevaFecha, null);

        Assert.Equal("2-2222-2222", estudiante.Cedula);
        Assert.Equal("Ana Lucía", estudiante.Nombre);
        Assert.Equal("Mora Solís", estudiante.Apellidos);
        Assert.Equal("77777777", estudiante.Telefono);
        Assert.Equal(nuevaFecha, estudiante.FechaNacimiento);
        Assert.Null(estudiante.Direccion);
        Assert.Equal(Hoy, estudiante.FechaRegistro);
    }
}
