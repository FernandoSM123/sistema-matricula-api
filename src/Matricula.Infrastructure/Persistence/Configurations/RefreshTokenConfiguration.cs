using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_token");

        builder.Property(t => t.Id).UseIdentityByDefaultColumn();

        // Solo el hash: el token en claro nunca llega a la BD.
        builder.Property(t => t.TokenHash).HasMaxLength(RefreshToken.TokenHashLength).IsFixedLength();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // La misma longitud que usuario.security_stamp.
        builder.Property(t => t.SecurityStamp).HasMaxLength(36);

        // Anulación de la familia ante un reuso (RN-51).
        builder.HasIndex(t => t.FamilyId);

        // Restrict, como todas las FK: eliminar a la persona borra antes sus sesiones (RN-35).
        builder.HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioId);

        // Dos renovaciones simultáneas con el mismo token: xmin hace fallar a una (punto 4 de la spec).
        builder.Property(t => t.Version).IsRowVersion();
    }
}
