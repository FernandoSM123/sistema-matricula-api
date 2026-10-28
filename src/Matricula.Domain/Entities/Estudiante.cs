using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Datos personales de un estudiante. El correo y el estado son los de su cuenta (RN-31, RN-38).
/// </summary>
public class Estudiante : EntidadBase
{
    // Para EF Core.
    private Estudiante()
    {
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        Usuario = null!;
    }

    public Estudiante(
        Usuario usuario,
        string cedula,
        string nombre,
        string apellidos,
        string telefono,
        DateOnly? fechaNacimiento,
        string? direccion,
        DateOnly fechaRegistro)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (usuario.Rol != Rol.Estudiante)
        {
            throw new ArgumentException("La cuenta de un estudiante debe tener el rol Estudiante.", nameof(usuario));
        }

        Usuario = usuario;
        FechaRegistro = fechaRegistro;
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        ActualizarDatos(cedula, nombre, apellidos, telefono, fechaNacimiento, direccion);
    }

    public string Cedula { get; private set; }
    public string Nombre { get; private set; }
    public string Apellidos { get; private set; }
    public string Telefono { get; private set; }
    public DateOnly? FechaNacimiento { get; private set; }
    public string? Direccion { get; private set; }

    /// <summary>Fecha de ingreso al instituto. Se fija al registrar y no cambia.</summary>
    public DateOnly FechaRegistro { get; private set; }

    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; }

    /// <summary>Edición por un administrador (RF-18).</summary>
    public void ActualizarDatos(
        string cedula,
        string nombre,
        string apellidos,
        string telefono,
        DateOnly? fechaNacimiento,
        string? direccion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cedula);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);

        Cedula = cedula.Trim();
        Nombre = nombre.Trim();
        Apellidos = apellidos.Trim();
        FechaNacimiento = fechaNacimiento;
        ActualizarContacto(telefono, direccion);
    }

    /// <summary>Lo único que el propio estudiante puede editar (matriz §2.2).</summary>
    public void ActualizarContacto(string telefono, string? direccion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(telefono);

        Telefono = telefono.Trim();
        Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
    }
}
