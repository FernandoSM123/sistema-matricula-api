using System.Linq.Expressions;
using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Periodos;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Grupos.Queries;

/// <summary>
/// Consultas 6, 7 y 8: los bloques de una semana de un estudiante, de un profesor o de un aula en un periodo. Sin periodo
/// se usa el vigente. Los grupos cancelados no cuentan.
/// </summary>
public class HorarioSemanalService(IAppDbContext db, PeriodoService periodoService, ICurrentUser currentUser)
{
    /// <summary>RF-28 para el administrador.</summary>
    public async Task<IReadOnlyList<BloqueHorarioResponse>> GetProfesorHorarioAsync(
        int profesorId, int? periodoId, CancellationToken cancellationToken)
    {
        if (!await db.Profesores.AnyAsync(p => p.Id == profesorId, cancellationToken))
        {
            throw new NotFoundException($"No existe el profesor {profesorId}.");
        }

        return await GetBloquesAsync(g => g.ProfesorId == profesorId, periodoId, cancellationToken);
    }

    /// <summary>RF-21 para el administrador: los grupos en que el estudiante tiene una matrícula activa.</summary>
    public async Task<IReadOnlyList<BloqueHorarioResponse>> GetEstudianteHorarioAsync(
        int estudianteId, int? periodoId, CancellationToken cancellationToken)
    {
        if (!await db.Estudiantes.AnyAsync(e => e.Id == estudianteId, cancellationToken))
        {
            throw new NotFoundException($"No existe el estudiante {estudianteId}.");
        }

        return await GetBloquesAsync(OfEstudiante(estudianteId), periodoId, cancellationToken);
    }

    /// <summary>
    /// Horario de la sesión: RF-21 para el estudiante y RF-28 para el profesor. La política del endpoint solo deja pasar
    /// a esos dos roles.
    /// </summary>
    public Task<IReadOnlyList<BloqueHorarioResponse>> GetMyHorarioAsync(int? periodoId, CancellationToken cancellationToken)
    {
        // Un token emitido antes de que existiera el claim no lo trae: basta con iniciar sesión de nuevo.
        if (currentUser.Rol == nameof(Rol.Estudiante))
        {
            var estudianteId = currentUser.EstudianteId
                ?? throw new ForbiddenException("Su sesión no identifica a un estudiante. Inicie sesión de nuevo.");

            return GetBloquesAsync(OfEstudiante(estudianteId), periodoId, cancellationToken);
        }

        var profesorId = currentUser.ProfesorId
            ?? throw new ForbiddenException("Su sesión no identifica a un profesor. Inicie sesión de nuevo.");

        return GetBloquesAsync(g => g.ProfesorId == profesorId, periodoId, cancellationToken);
    }

    /// <summary>RF-41: los bloques en que el aula está ocupada.</summary>
    public async Task<IReadOnlyList<BloqueHorarioResponse>> GetAulaOcupacionAsync(
        int aulaId, int? periodoId, CancellationToken cancellationToken)
    {
        if (!await db.Aulas.AnyAsync(a => a.Id == aulaId, cancellationToken))
        {
            throw new NotFoundException($"No existe el aula {aulaId}.");
        }

        return await GetBloquesAsync(g => g.AulaId == aulaId, periodoId, cancellationToken);
    }

    private async Task<IReadOnlyList<BloqueHorarioResponse>> GetBloquesAsync(
        Expression<Func<Grupo, bool>> filtro, int? periodoId, CancellationToken cancellationToken)
    {
        var periodo = await ResolvePeriodoAsync(periodoId, cancellationToken);

        var bloques = await db.Grupos.AsNoTracking()
            .Where(filtro)
            .Where(g => g.PeriodoId == periodo && g.Estado != EstadoGrupo.Cancelado)
            .SelectMany(g => g.Horarios.Select(h => new BloqueHorarioResponse(
                h.Dia,
                h.HoraInicio,
                h.HoraFin,
                g.Id,
                g.Nrc,
                g.Curso.Codigo,
                g.Curso.Nombre,
                g.ProfesorId,
                g.Profesor.Nombre + " " + g.Profesor.Apellidos,
                g.Aula != null ? g.Aula.Codigo : null,
                g.Modalidad,
                g.Estado)))
            .ToListAsync(cancellationToken);

        // El día se guarda como texto: en SQL se ordenaría alfabéticamente.
        return [.. bloques.OrderBy(b => b.Dia).ThenBy(b => b.HoraInicio).ThenBy(b => b.Nrc)];
    }

    private Expression<Func<Grupo, bool>> OfEstudiante(int estudianteId) =>
        g => db.Matriculas.Any(m =>
            m.GrupoId == g.Id && m.EstudianteId == estudianteId && m.Estado == EstadoMatricula.Activa);

    private async Task<int> ResolvePeriodoAsync(int? periodoId, CancellationToken cancellationToken)
    {
        if (periodoId is { } id)
        {
            return await db.Periodos.AnyAsync(p => p.Id == id, cancellationToken)
                ? id
                : throw new NotFoundException($"No existe el periodo {id}.");
        }

        return await periodoService.GetVigenteIdAsync(cancellationToken)
            ?? throw new NotFoundException("No hay un periodo vigente. Indique el periodoId a consultar.");
    }
}
