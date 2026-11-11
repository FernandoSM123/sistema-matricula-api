using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Constancia de un cambio de datos (RF-56, §5.12). No hereda de EntidadBase porque nunca se modifica (RN-43), y no
/// tiene relaciones: describe registros que pueden eliminarse después y cuentas que pueden dejar de existir.
/// </summary>
public class RegistroAuditoria
{
    // Para EF Core.
    private RegistroAuditoria()
    {
        Entidad = Cambios = OperacionId = string.Empty;
    }

    public RegistroAuditoria(
        DateTimeOffset fecha,
        AccionAuditoria accion,
        string entidad,
        int entidadId,
        string cambios,
        int? usuarioId,
        string? usuarioCorreo,
        Rol? usuarioRol,
        string operacionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(cambios);
        ArgumentException.ThrowIfNullOrWhiteSpace(operacionId);

        Fecha = fecha;
        Accion = accion;
        Entidad = entidad;
        EntidadId = entidadId;
        Cambios = cambios;
        UsuarioId = usuarioId;
        UsuarioCorreo = usuarioCorreo;
        UsuarioRol = usuarioRol;
        OperacionId = operacionId;
    }

    public long Id { get; private set; }

    /// <summary>Momento del guardado, en UTC. Es el mismo para todos los registros de un guardado.</summary>
    public DateTimeOffset Fecha { get; private set; }

    public AccionAuditoria Accion { get; private set; }

    /// <summary>Nombre de la clase del registro afectado, por ejemplo Matricula.</summary>
    public string Entidad { get; private set; }

    public int EntidadId { get; private set; }

    /// <summary>JSON con los valores o las diferencias, armado por <see cref="Reglas.ReglasAuditoria"/>.</summary>
    public string Cambios { get; private set; }

    /// <summary>Autor del cambio; null es el Sistema (RN-45). Sin FK: la cuenta puede eliminarse después.</summary>
    public int? UsuarioId { get; private set; }

    /// <summary>Correo del autor en el momento del cambio (RN-45).</summary>
    public string? UsuarioCorreo { get; private set; }

    public Rol? UsuarioRol { get; private set; }

    /// <summary>Agrupa los registros producidos por una misma operación (RN-42).</summary>
    public string OperacionId { get; private set; }
}