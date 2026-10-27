using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("estudiante");

        // RN-13: la cédula es única dentro de esta tabla, no entre estudiantes, profesores y administradores.
        builder.Property(e => e.Cedula).HasMaxLength(20);
        builder.HasIndex(e => e.Cedula).IsUnique();

        builder.Property(e => e.Nombre).HasMaxLength(100);
        builder.Property(e => e.Apellidos).HasMaxLength(100);
        builder.Property(e => e.Telefono).HasMaxLength(20);
        builder.Property(e => e.Direccion).HasMaxLength(250);

        // 1:1 con su cuenta: EF crea el índice único sobre usuario_id.
        builder.HasOne(e => e.Usuario)
            .WithOne()
            .HasForeignKey<Estudiante>(e => e.UsuarioId);
    }
}
