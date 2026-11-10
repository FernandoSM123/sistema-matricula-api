using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matricula.Infrastructure.Persistence.Configurations;

public class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("horario");

        // La relación con grupo (y el índice en grupo_id) se define en GrupoConfiguration. Dia (texto) sigue las
        // convenciones de AppDbContext; HoraInicio y HoraFin se guardan como time.
    }
}
