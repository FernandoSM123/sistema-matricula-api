using System.Linq.Expressions;
using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Grupos;

/// <summary>
/// Guardas que el módulo grupos añade a profesores, cursos y aulas (decisión 4 del mapa).
/// </summary>
public static class GuardasGrupos
{
    /// <summary>
    /// Grupos que todavía usan su profesor, curso y aula: Planificado, Abierto, Cerrado o EnCurso (RN-37 y RN-09).
    /// </summary>
    public static readonly Expression<Func<Grupo, bool>> Activo =
        g => g.Estado != EstadoGrupo.Finalizado && g.Estado != EstadoGrupo.Cancelado;

    /// <summary>
    /// RN-37: no se desactiva un profesor, curso o aula con grupos activos. <paramref name="descripcion"/> completa
    /// "No se puede desactivar ..." (por ejemplo, "el curso PROG-101").
    /// </summary>
    public static async Task ValidarSinGruposActivosAsync(
        this IAppDbContext db,
        Expression<Func<Grupo, bool>> filtro,
        string descripcion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var cantidad = await db.Grupos.Where(Activo).Where(filtro).CountAsync(cancellationToken);
        if (cantidad > 0)
        {
            throw new BusinessRuleException(
                "RN-37",
                $"No se puede desactivar {descripcion}: tiene {cantidad} grupo(s) Planificado, Abierto, Cerrado o "
                + "EnCurso. Reasígnelos o cancélelos primero.");
        }
    }
}
