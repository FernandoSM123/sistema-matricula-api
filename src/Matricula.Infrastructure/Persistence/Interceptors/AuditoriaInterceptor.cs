using Matricula.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Matricula.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Asigna FechaCreacion y FechaModificacion automáticamente al guardar.
/// </summary>
public class AuditoriaInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            var ahora = timeProvider.GetUtcNow();
            foreach (var entry in eventData.Context.ChangeTracker.Entries<EntidadBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = ahora;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.FechaModificacion = ahora;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
