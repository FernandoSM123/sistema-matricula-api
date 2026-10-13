using Matricula.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // public DbSet<Estudiante> Estudiantes => Set<Estudiante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Nunca se elimina en cascada (sección 4.2 de los requerimientos).
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Los enums se guardan como texto para que la BD sea legible.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }
}
