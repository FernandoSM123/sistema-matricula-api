using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class PeriodoConfiguration : IEntityTypeConfiguration<Periodo>
{
    public void Configure(EntityTypeBuilder<Periodo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("periodo");

        // RN-13: el nombre se guarda en mayúsculas, así el índice no distingue capitalización.
        builder.Property(p => p.Nombre).HasMaxLength(50);
        builder.HasIndex(p => p.Nombre).IsUnique();
    }
}
