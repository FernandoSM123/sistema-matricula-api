namespace Matricula.Application.Abstractions;

/// <summary>
/// Acceso a datos para los servicios. Expone los DbSet de cada entidad y hace las veces de Unit of Work.
/// </summary>
public interface IAppDbContext
{
    // Agregar aquí los DbSet<> a medida que se creen las entidades, por ejemplo:
    // DbSet<Estudiante> Estudiantes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
