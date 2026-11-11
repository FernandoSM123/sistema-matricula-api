using System.Diagnostics;
using Matricula.Application.Abstractions;
using Matricula.Domain.Common;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Matricula.Infrastructure.Persistence.Auditoria;

/// <summary>
/// Lee el ChangeTracker antes de un guardado y prepara los registros de auditoría (RF-56). Qué se guarda de cada
/// propiedad lo decide <see cref="ReglasAuditoria"/>; aquí solo se traduce EF a nombres y valores.
/// </summary>
public class CapturaAuditoria(ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<CambiosPendientes> CapturarAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Entries() detecta antes los cambios hechos sobre las entidades rastreadas sin marcarlas.
        var cambios = new List<CambioCapturado>();
        var creadas = new List<EntityEntry<EntidadBase>>();
        foreach (var entrada in context.ChangeTracker.Entries<EntidadBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    creadas.Add(entrada);
                    break;

                case EntityState.Modified:
                    // Solo aquí se conocen los valores originales: después del guardado se reinician.
                    var diferencias = ReglasAuditoria.Diferencias(
                        NombreEntidad(entrada),
                        entrada.Properties
                            .Where(p => p.IsModified)
                            .Select(p => new PropiedadAuditada(p.Metadata.Name, p.OriginalValue, p.CurrentValue)));
                    if (diferencias is not null)
                    {
                        cambios.Add(new CambioCapturado(
                            AccionAuditoria.Modificacion, NombreEntidad(entrada), entrada.Entity.Id, diferencias));
                    }

                    break;

                case EntityState.Deleted:
                    cambios.Add(new CambioCapturado(
                        AccionAuditoria.Eliminacion, NombreEntidad(entrada), entrada.Entity.Id, Valores(entrada)));
                    break;
            }
        }

        if (cambios.Count == 0 && creadas.Count == 0)
        {
            return CambiosPendientes.Ninguno;
        }

        var autor = await AutorAsync(context, cancellationToken);
        return new CambiosPendientes(timeProvider.GetUtcNow(), autor, OperacionActual(), cambios, creadas);
    }

    internal static string NombreEntidad(EntityEntry<EntidadBase> entrada) => entrada.Metadata.ClrType.Name;

    /// <summary>Creación o eliminación: los valores actuales de todas las propiedades escalares.</summary>
    internal static string Valores(EntityEntry<EntidadBase> entrada) =>
        ReglasAuditoria.Valores(
            NombreEntidad(entrada),
            entrada.Properties.Select(p => new KeyValuePair<string, object?>(p.Metadata.Name, p.CurrentValue)));

    /// <summary>
    /// RN-45 y supuesto 3: el correo se lee de la cuenta, no del token, porque el del token puede estar desactualizado
    /// después de un cambio de correo. Se lee antes de guardar: si el autor cambia su propio correo en este guardado,
    /// queda el que tenía al hacerlo.
    /// </summary>
    private async Task<AutorCambio> AutorAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not { } usuarioId)
        {
            return AutorCambio.Sistema;
        }

        var correo = await context.Set<Usuario>()
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Correo)
            .FirstOrDefaultAsync(cancellationToken);
        Rol? rol = Enum.TryParse<Rol>(currentUser.Rol, out var leido) ? leido : null;

        return new AutorCambio(usuarioId, correo, rol);
    }

    /// <summary>
    /// RN-42 y supuesto 4: el trace id de la petición, el mismo que forma parte del traceId de los ProblemDetails.
    /// Fuera de una petición (por ejemplo, el seed) se genera uno por guardado.
    /// </summary>
    private static string OperacionActual() =>
        Activity.Current is { TraceId: var traceId } && traceId != default
            ? traceId.ToHexString()
            : ActivityTraceId.CreateRandom().ToHexString();
}