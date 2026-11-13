using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Application.Abstractions;

/// <summary>
/// Acceso a datos para los servicios. Expone los DbSet de cada entidad y hace las veces de Unit of Work.
/// </summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Administrador> Administradores { get; }
    DbSet<Estudiante> Estudiantes { get; }
    DbSet<Profesor> Profesores { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Aula> Aulas { get; }
    DbSet<Periodo> Periodos { get; }
    DbSet<Grupo> Grupos { get; }
    DbSet<Horario> Horarios { get; }
    DbSet<MatriculaEntidad> Matriculas { get; }

    /// <summary>
    /// Auditoría de cambios, solo para leer (RN-43): los registros los agrega AppDbContext al guardar, nunca un servicio.
    /// </summary>
    IQueryable<RegistroAuditoria> RegistrosAuditoria { get; }

    /// <summary>
    /// Errores inesperados, solo para leer (RN-49): los registra Infrastructure, nunca un servicio.
    /// </summary>
    IQueryable<RegistroError> RegistrosError { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}