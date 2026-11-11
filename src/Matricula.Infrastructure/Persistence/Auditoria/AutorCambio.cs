using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Persistence.Auditoria;

/// <summary>
/// Quién hizo los cambios de un guardado, copiado en ese momento (RN-45). Todo null es el Sistema.
/// </summary>
internal sealed record AutorCambio(int? UsuarioId, string? Correo, Rol? Rol)
{
    public static readonly AutorCambio Sistema = new(null, null, null);
}