using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Infrastructure.Persistence.Auditoria;
using Microsoft.EntityFrameworkCore;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, CapturaAuditoria captura)
    : DbContext(options), IAppDbContext
{
    // Un DbSet<T> representa una tabla y es el punto de entrada para consultarla o agregarle filas.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<Periodo> Periodos => Set<Periodo>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<MatriculaEntidad> Matriculas => Set<MatriculaEntidad>();

    // Sin DbSet público: Application solo puede leer la auditoría (RN-43). La entidad entra al modelo por
    // RegistroAuditoriaConfiguration.
    IQueryable<RegistroAuditoria> IAppDbContext.RegistrosAuditoria => Set<RegistroAuditoria>().AsNoTracking();

    // Igual que la auditoría: Application solo lee los errores (RN-49). Los escriben RegistroErroresEnBd y
    // DepuracionErrores, en Infrastructure. La entidad entra al modelo por RegistroErrorConfiguration.
    IQueryable<RegistroError> IAppDbContext.RegistrosError => Set<RegistroError>().AsNoTracking();

    /// <summary>
    /// Guarda los cambios junto con su auditoría (RF-56, RN-42), en una sola transacción: o quedan los dos o ninguno.
    /// Detalle del flujo en docs/arquitectura.md §5.1.
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pendientes = await captura.CapturarAsync(this, cancellationToken);
        if (pendientes.EstaVacio)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Si quien llama ya abrió una transacción, la confirma él. Sin confirmar, el Dispose la revierte.
        await using var transaccion = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Siempre acepta los cambios: si no, el segundo guardado volvería a insertar las entidades del primero.
        var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
        Set<RegistroAuditoria>().AddRange(pendientes.Completar());
        await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

        if (transaccion is not null)
        {
            await transaccion.CommitAsync(cancellationToken);
        }

        return filas;
    }

    /// <summary>
    /// Supuesto 12 de la spec de auditoria: ningún servicio guarda de forma síncrona, y así ningún guardado se salta la
    /// auditoría por error.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("Use SaveChangesAsync: el guardado síncrono no registra la auditoría.");


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
