using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuario");

        builder.Property(u => u.Correo).HasMaxLength(256);
        builder.HasIndex(u => u.Correo).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.TempPasswordHash).HasMaxLength(200);
        builder.Property(u => u.SecurityStamp).HasMaxLength(36);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(u => u.CreadoPorId);
    }
}