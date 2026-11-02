using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Datos personales de un profesor. El correo y el estado son los de su cuenta (RN-31, RN-38).
/// </summary>
public class Profesor : EntidadBase
{
    // Para EF Core.
    private Profesor()
    {
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        Usuario = null!;
    }

    public Profesor(
        Usuario usuario,
        string cedula,
        string nombre,
        string apellidos,
        string telefono,
        string? especialidad,
        DateOnly? fechaContratacion)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (usuario.Rol != Rol.Profesor)
        {
            throw new ArgumentException("La cuenta de un profesor debe tener el rol Profesor.", nameof(usuario));
        }

        Usuario = usuario;
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        ActualizarDatos(cedula, nombre, apellidos, telefono, especialidad, fechaContratacion);
    }

    public string Cedula { get; private set; }
    public string Nombre { get; private set; }
    public string Apellidos { get; private set; }
    public string Telefono { get; private set; }
    public string? Especialidad { get; private set; }
    public DateOnly? FechaContratacion { get; private set; }

    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; }

    /// <summary>Edición por un administrador (RF-25).</summary>
    public void ActualizarDatos(
        string cedula,
        string nombre,
        string apellidos,
        string telefono,
        string? especialidad,
        DateOnly? fechaContratacion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cedula);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);
        ArgumentException.ThrowIfNullOrWhiteSpace(telefono);

        Cedula = cedula.Trim();
        Nombre = nombre.Trim();
        Apellidos = apellidos.Trim();
        Telefono = telefono.Trim();
        Especialidad = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();
        FechaContratacion = fechaContratacion;
    }
}
