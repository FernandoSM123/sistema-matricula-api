using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class GrupoConfiguration : IEntityTypeConfiguration<Grupo>
{
    public void Configure(EntityTypeBuilder<Grupo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("grupo");

        // RN-13: el NRC se guarda en mayúsculas, así el índice no distingue capitalización.
        builder.Property(g => g.Nrc).HasMaxLength(10);
        builder.HasIndex(g => g.Nrc).IsUnique();

        builder.HasOne(g => g.Curso).WithMany().HasForeignKey(g => g.CursoId);
        builder.HasOne(g => g.Profesor).WithMany().HasForeignKey(g => g.ProfesorId);
        builder.HasOne(g => g.Periodo).WithMany().HasForeignKey(g => g.PeriodoId);
        builder.HasOne(g => g.Aula).WithMany().HasForeignKey(g => g.AulaId).IsRequired(false);

        // Búsqueda de traslapes (RN-07, RN-08): bloques de otros grupos del mismo periodo y profesor o aula.
        builder.HasIndex(g => new { g.PeriodoId, g.ProfesorId });
        builder.HasIndex(g => new { g.PeriodoId, g.AulaId });

        // Los bloques se agregan y quitan solo a través de Grupo (RF-48).
        builder.HasMany(g => g.Horarios).WithOne().HasForeignKey(h => h.GrupoId);
        builder.Navigation(g => g.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(g => g.CuposDisponibles);

        // Concurrencia del último cupo: Npgsql mapea un uint con IsRowVersion a la columna de sistema xmin, que
        // PostgreSQL cambia en cada UPDATE. No crea columna.
        builder.Property(g => g.Version).IsRowVersion();

        // Estado y Modalidad (texto) siguen las convenciones de AppDbContext.
    }
}
