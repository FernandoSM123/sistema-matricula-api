using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class RegistroErrorConfiguration : IEntityTypeConfiguration<RegistroError>
{
    public void Configure(EntityTypeBuilder<RegistroError> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("registro_error");

        builder.Property(r => r.Id).UseIdentityByDefaultColumn();
        builder.Property(r => r.Descripcion).HasMaxLength(RegistroError.MaxDescripcion);
        builder.Property(r => r.TipoExcepcion).HasMaxLength(RegistroError.MaxTipoExcepcion);
        builder.Property(r => r.Mensaje).HasMaxLength(RegistroError.MaxMensaje);
        // Su límite lo pone el recorte del dominio, no la columna.
        builder.Property(r => r.Detalle).HasColumnType("text");
        builder.Property(r => r.Metodo).HasMaxLength(RegistroError.MaxMetodo);
        builder.Property(r => r.Ruta).HasMaxLength(RegistroError.MaxRuta);
        builder.Property(r => r.TraceId).HasMaxLength(RegistroError.MaxTraceId);

        // Sin relaciones, como la auditoría: el usuario puede eliminarse después.

        // Orden y filtro por fecha, y depuración (RN-49).
        builder.HasIndex(r => r.Fecha);
        // No único: un error de correo y un 500 de la misma petición comparten el trace id.
        builder.HasIndex(r => r.TraceId);

        // Origen y UsuarioRol (texto) siguen las convenciones de AppDbContext.
    }
}
