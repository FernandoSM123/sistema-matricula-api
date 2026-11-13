using Matricula.Domain.Common;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Matricula.Infrastructure.Persistence.Auditoria;

/// <summary>
/// Lo que <see cref="CapturaAuditoria"/> encontró antes de un guardado. Las modificaciones y eliminaciones ya están
/// armadas; las creaciones se arman en <see cref="Completar"/>, después del guardado, cuando ya tienen su Id y sus FK
/// definitivas (antes, una FK hacia otra entidad nueva del mismo guardado tiene un id temporal).
/// </summary>
public sealed class CambiosPendientes
{
    public static readonly CambiosPendientes Ninguno = new(default, AutorCambio.Sistema, string.Empty, [], []);

    private readonly DateTimeOffset _fecha;
    private readonly AutorCambio _autor;
    private readonly string _operacionId;
    private readonly IReadOnlyList<CambioCapturado> _cambios;
    private readonly IReadOnlyList<EntityEntry<EntidadBase>> _creadas;

    internal CambiosPendientes(
        DateTimeOffset fecha,
        AutorCambio autor,
        string operacionId,
        IReadOnlyList<CambioCapturado> cambios,
        IReadOnlyList<EntityEntry<EntidadBase>> creadas)
    {
        _fecha = fecha;
        _autor = autor;
        _operacionId = operacionId;
        _cambios = cambios;
        _creadas = creadas;
    }

    /// <summary>Nada que auditar: el guardado sigue como siempre, sin transacción propia.</summary>
    public bool EstaVacio => _cambios.Count == 0 && _creadas.Count == 0;

    /// <summary>
    /// Todos los registros del guardado, con la misma fecha, el mismo autor y la misma operación (RN-42, RN-45). Se
    /// llama después del primer guardado.
    /// </summary>
    public IReadOnlyList<RegistroAuditoria> Completar()
    {
        var creaciones = _creadas.Select(entrada => new CambioCapturado(
            AccionAuditoria.Creacion,
            CapturaAuditoria.NombreEntidad(entrada),
            entrada.Entity.Id,
            CapturaAuditoria.Valores(entrada)));

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