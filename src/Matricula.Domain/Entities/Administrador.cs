using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Datos personales de un administrador. El correo y el estado son los de su cuenta (RN-31, RN-38).
/// </summary>
public class Administrador : EntidadBase
{
    // Para EF Core.
    private Administrador()
    {
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        Usuario = null!;
    }

    public Administrador(Usuario usuario, string cedula, string nombre, string apellidos, string telefono)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (usuario.Rol != Rol.Administrador)
        {
            throw new ArgumentException("La cuenta de un administrador debe tener el rol Administrador.", nameof(usuario));
        }

        Usuario = usuario;
        Cedula = Nombre = Apellidos = Telefono = string.Empty;
        ActualizarDatos(cedula, nombre, apellidos, telefono);
    }

    public string Cedula { get; private set; }
    public string Nombre { get; private set; }
    public string Apellidos { get; private set; }
    public string Telefono { get; private set; }

    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; }

    public void ActualizarDatos(string cedula, string nombre, string apellidos, string telefono)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cedula);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);
        ArgumentException.ThrowIfNullOrWhiteSpace(telefono);

        Cedula = cedula.Trim();
        Nombre = nombre.Trim();
        Apellidos = apellidos.Trim();
        Telefono = telefono.Trim();
    }
}