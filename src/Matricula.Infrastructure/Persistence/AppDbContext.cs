using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // Un DbSet<T> representa una tabla y es el punto de entrada para consultarla o agregarle filas.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();


    // cómo se mapean las clases a tablas
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Este método busca todas las clases IEntityTypeConfiguration<T> del proyecto 
        // (tus UsuarioConfiguration y AdministradorConfiguration, etc) 
        // y las aplica. En UsuarioConfiguration, por ejemplo, defines

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Nunca se elimina en cascada (sección 4.2 de los requerimientos).
        // Recorre todas las claves foráneas y les quita el borrado en cascada (es una regla de tu proyecto). 
        // Si intentas borrar un padre que tiene hijos, la BD lo rechaza.

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    // Son reglas para todas las propiedades de un tipo. Por ejemplo, cualquier enum (como Rol) 
    // se guarda como texto ("Administrador") y no como número.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Los enums se guardan como texto para que la BD sea legible.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }
}
