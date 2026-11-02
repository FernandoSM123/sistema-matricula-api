using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class ProfesorConfiguration : IEntityTypeConfiguration<Profesor>
{
    public void Configure(EntityTypeBuilder<Profesor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("profesor");

        // RN-13: la cédula es única dentro de esta tabla, no entre estudiantes, profesores y administradores.
        builder.Property(p => p.Cedula).HasMaxLength(20);
        builder.HasIndex(p => p.Cedula).IsUnique();

        builder.Property(p => p.Nombre).HasMaxLength(100);
        builder.Property(p => p.Apellidos).HasMaxLength(100);
        builder.Property(p => p.Telefono).HasMaxLength(20);
        builder.Property(p => p.Especialidad).HasMaxLength(100);

        // 1:1 con su cuenta: EF crea el índice único sobre usuario_id.
        builder.HasOne(p => p.Usuario)
            .WithOne()
            .HasForeignKey<Profesor>(p => p.UsuarioId);
    }
}
