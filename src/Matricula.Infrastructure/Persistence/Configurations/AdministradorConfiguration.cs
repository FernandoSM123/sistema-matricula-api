using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class AdministradorConfiguration : IEntityTypeConfiguration<Administrador>
{
    public void Configure(EntityTypeBuilder<Administrador> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("administrador");

        // RN-13: la cédula es única dentro de esta tabla, no entre estudiantes, profesores y administradores.
        builder.Property(a => a.Cedula).HasMaxLength(20);
        builder.HasIndex(a => a.Cedula).IsUnique();

        builder.Property(a => a.Nombre).HasMaxLength(100);
        builder.Property(a => a.Apellidos).HasMaxLength(100);
        builder.Property(a => a.Telefono).HasMaxLength(20);

        // 1:1 con su cuenta: EF crea el índice único sobre usuario_id.
        builder.HasOne(a => a.Usuario)
            .WithOne()
            .HasForeignKey<Administrador>(a => a.UsuarioId);
    }
}