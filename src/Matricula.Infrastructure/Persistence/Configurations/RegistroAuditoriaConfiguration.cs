using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("registro_auditoria");

        builder.Property(r => r.Id).UseIdentityByDefaultColumn();
        builder.Property(r => r.Entidad).HasMaxLength(50);
        builder.Property(r => r.Cambios).HasColumnType("jsonb");
        builder.Property(r => r.UsuarioCorreo).HasMaxLength(256);   // igual que usuario.correo
        builder.Property(r => r.OperacionId).HasMaxLength(32);      // trace id en hexadecimal

        // Sin relaciones a propósito: una FK (todas son Restrict) impediría eliminar estudiantes o profesores junto con
        // su cuenta (RN-35), y los registros deben quedar después de eliminar el dato (RN-43).

        // Filtros de RF-57.
        builder.HasIndex(r => new { r.Entidad, r.EntidadId });
        builder.HasIndex(r => r.UsuarioId);
        builder.HasIndex(r => r.Fecha);

        // Accion y UsuarioRol (texto) siguen las convenciones de AppDbContext.
    }
}