using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class AulaConfiguration : IEntityTypeConfiguration<Aula>
{
    public void Configure(EntityTypeBuilder<Aula> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("aula");

        // RN-13: el código se guarda en mayúsculas, así el índice no distingue capitalización.
        builder.Property(a => a.Codigo).HasMaxLength(20);
        builder.HasIndex(a => a.Codigo).IsUnique();

        builder.Property(a => a.Ubicacion).HasMaxLength(150);
    }
}
