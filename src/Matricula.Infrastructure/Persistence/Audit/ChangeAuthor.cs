using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Persistence.Audit;

/// <summary>
/// Quién hizo los cambios de un guardado, copiado en ese momento (RN-45). Todo null es el Sistema.
/// </summary>
internal sealed record ChangeAuthor(int? UsuarioId, string? Correo, Rol? Rol)
{
    public static readonly ChangeAuthor System = new(null, null, null);
}