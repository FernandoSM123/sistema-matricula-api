using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class CursoConfiguration : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("curso");

        // RN-13: el código se guarda en mayúsculas, así el índice no distingue capitalización.
        builder.Property(c => c.Codigo).HasMaxLength(20);
        builder.HasIndex(c => c.Codigo).IsUnique();

        builder.Property(c => c.Nombre).HasMaxLength(150);
        builder.Property(c => c.Descripcion).HasMaxLength(1000);

        // Nivel (texto) y Costo (numeric(10,2)) siguen las convenciones de AppDbContext.
    }
}
