using Matricula.Domain.Common;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Matricula.Infrastructure.Persistence.Audit;

/// <summary>
/// Lo que <see cref="AuditCapture"/> encontró antes de un guardado. Las modificaciones y eliminaciones ya están
/// armadas; las creaciones se arman en <see cref="Completar"/>, después del guardado, cuando ya tienen su Id y sus FK
/// definitivas (antes, una FK hacia otra entidad nueva del mismo guardado tiene un id temporal).
/// </summary>
public sealed class PendingChanges
{
    public static readonly PendingChanges None = new(default, ChangeAuthor.System, string.Empty, [], []);

    private readonly DateTimeOffset _fecha;
    private readonly ChangeAuthor _autor;
    private readonly string _operacionId;
    private readonly IReadOnlyList<CapturedChange> _cambios;
    private readonly IReadOnlyList<EntityEntry<EntidadBase>> _creadas;

    internal PendingChanges(
        DateTimeOffset fecha,
        ChangeAuthor autor,
        string operacionId,
        IReadOnlyList<CapturedChange> cambios,
        IReadOnlyList<EntityEntry<EntidadBase>> creadas)
    {
        _fecha = fecha;
        _autor = autor;
        _operacionId = operacionId;
        _cambios = cambios;
        _creadas = creadas;
    }

    /// <summary>Nada que auditar: el guardado sigue como siempre, sin transacción propia.</summary>
    public bool IsEmpty => _cambios.Count == 0 && _creadas.Count == 0;

    /// <summary>
    /// Todos los registros del guardado, con la misma fecha, el mismo autor y la misma operación (RN-42, RN-45). Se
    /// llama después del primer guardado.
    /// </summary>
    public IReadOnlyList<RegistroAuditoria> Complete()
    {
        var creaciones = _creadas.Select(entrada => new CapturedChange(
            AccionAuditoria.Creacion,
            AuditCapture.EntityName(entrada),
            entrada.Entity.Id,
            AuditCapture.Values(entrada)));

        return _cambios
            .Concat(creaciones)
            .Select(cambio => new RegistroAuditoria(
                _fecha,
                cambio.Accion,
                cambio.Entidad,
                cambio.EntidadId,
                cambio.Cambios,
                _autor.UsuarioId,
                _autor.Correo,
                _autor.Rol,
                _operacionId))
            .ToList();
    }
}