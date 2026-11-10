using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class MatriculaConfiguration : IEntityTypeConfiguration<MatriculaEntidad>
{
    public void Configure(EntityTypeBuilder<MatriculaEntidad> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("matricula");

        builder.HasOne(m => m.Estudiante).WithMany().HasForeignKey(m => m.EstudianteId);
        builder.HasOne(m => m.Grupo).WithMany().HasForeignKey(m => m.GrupoId);

        // Una sola matrícula activa por estudiante y grupo; después de retirarse se puede volver a matricular. El
        // filtro es SQL de PostgreSQL: el estado se guarda como texto (convenciones de AppDbContext).
        builder.HasIndex(m => new { m.EstudianteId, m.GrupoId })
            .IsUnique()
            .HasFilter($"estado = '{nameof(EstadoMatricula.Activa)}'");

        // Estudiantes de un grupo (RF-49) y matrículas activas al cancelar (RN-12).
        builder.HasIndex(m => new { m.GrupoId, m.Estado });

        // Estado y MotivoRetiro (texto) siguen las convenciones de AppDbContext.
    }
}
