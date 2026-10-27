using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Abstractions;

/// <summary>
/// Acceso a datos para los servicios. Expone los DbSet de cada entidad y hace las veces de Unit of Work.
/// </summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Administrador> Administradores { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}