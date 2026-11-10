using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Matricula.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Reglas de matrícula que necesitan la BD y comparten matricular, cambiar de grupo y agregar un bloque a un grupo
/// con matriculados.
/// </summary>
public static class GuardasMatriculas
{
    /// <summary>
    /// RN-05: el estudiante no tiene otra matrícula activa en el mismo curso y periodo (incluido el mismo grupo).
    /// <paramref name="ignorarMatriculaId"/> excluye la matrícula de origen de un cambio de grupo.
    /// </summary>
    public static async Task ValidarCursoNoRepetidoAsync(
        this IAppDbContext db,
        int estudianteId,
        Grupo grupo,
        int? ignorarMatriculaId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(grupo);

        var nrc = await db.Matriculas.AsNoTracking()
            .Where(m => m.EstudianteId == estudianteId
                && m.Estado == EstadoMatricula.Activa
                && m.Id != ignorarMatriculaId
                && m.Grupo.CursoId == grupo.CursoId
                && m.Grupo.PeriodoId == grupo.PeriodoId)
            .Select(m => m.Grupo.Nrc)
            .FirstOrDefaultAsync(cancellationToken);

        if (nrc is null)
        {
            return;
        }

        throw new BusinessRuleException(
            "RN-05",
            nrc == grupo.Nrc
                ? $"El estudiante ya está matriculado en el grupo {nrc}."
                : $"El estudiante ya está matriculado en el grupo {nrc} del mismo curso y periodo.");
    }

    /// <summary>
    /// RN-06 (supuesto 4 de la spec): los choques de <paramref name="bloques"/> con los grupos donde cada estudiante
    /// tiene una matrícula activa, en periodos cuyas clases se traslapan con <paramref name="periodo"/>. No cuentan
    /// los grupos Cancelado ni Finalizado, ni el grupo <paramref name="excluirGrupoId"/> (el que recibe los bloques), ni
    /// la matrícula <paramref name="ignorarMatriculaId"/> (el origen de un cambio de grupo).
    /// </summary>
    public static async Task<IReadOnlyList<ChoqueHorario>> BuscarChoquesAsync(
        this IAppDbContext db,
        IReadOnlyCollection<int> estudianteIds,
        IReadOnlyCollection<Horario> bloques,
        Periodo periodo,
        int excluirGrupoId,
        int? ignorarMatriculaId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(estudianteIds);
        ArgumentNullException.ThrowIfNull(bloques);
        ArgumentNullException.ThrowIfNull(periodo);

        if (estudianteIds.Count == 0 || bloques.Count == 0)
        {
            return [];
        }

        var dias = bloques.Select(b => b.Dia).Distinct().ToList();
        var inicio = periodo.InicioClases;
        var fin = periodo.FinClases;

        // Mismas comparaciones que ReglasPeriodo.ClasesSeTraslapan, expresadas para SQL.
        var ocupados = await db.Matriculas.AsNoTracking()
            .Where(m => estudianteIds.Contains(m.EstudianteId)
                && m.Estado == EstadoMatricula.Activa
                && m.Id != ignorarMatriculaId
                && m.GrupoId != excluirGrupoId
                && m.Grupo.Estado != EstadoGrupo.Cancelado
                && m.Grupo.Estado != EstadoGrupo.Finalizado
                && m.Grupo.Periodo.InicioClases <= fin
                && inicio <= m.Grupo.Periodo.FinClases)
            .SelectMany(m => m.Grupo.Horarios
                .Where(h => dias.Contains(h.Dia))
                .Select(h => new ChoqueHorario(m.EstudianteId, m.Grupo.Nrc, h.Dia, h.HoraInicio, h.HoraFin)))
            .ToListAsync(cancellationToken);

        return [.. ocupados.Where(o => bloques.Any(b =>
            ReglasHorario.SeTraslapan(b.Dia, b.HoraInicio, b.HoraFin, o.Dia, o.HoraInicio, o.HoraFin)))];
    }

    /// <summary>RN-06 para un estudiante: el mensaje nombra el grupo y el bloque con que choca.</summary>
    public static async Task ValidarSinChoquesAsync(
        this IAppDbContext db,
        int estudianteId,
        Grupo grupo,
        int? ignorarMatriculaId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grupo);

        var choques = await db.BuscarChoquesAsync(
            [estudianteId], grupo.Horarios, grupo.Periodo, grupo.Id, ignorarMatriculaId, cancellationToken);

        if (choques.Count == 0)
        {
            return;
        }

        var c = choques[0];
        throw new BusinessRuleException(
            "RN-06",
            $"El horario del grupo {grupo.Nrc} choca con el grupo {c.Nrc}, en el que el estudiante está matriculado, "
            + $"el {c.Dia} de {c.HoraInicio:HH\\:mm} a {c.HoraFin:HH\\:mm}.");
    }
}
