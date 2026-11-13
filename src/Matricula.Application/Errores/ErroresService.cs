using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Errores;

/// <summary>
/// Consulta de los errores inesperados (RF-59). Solo lee: los registros los escribe Infrastructure (RN-49).
/// </summary>
public class ErroresService(IAppDbContext db, IFechaLocal fechaLocal)
{
    /// <summary>
    /// Filtra con todos los criterios recibidos y ordena del error más reciente al más antiguo, sin el detalle.
    /// </summary>
    public async Task<PagedResult<RegistroErrorResponse>> ListarAsync(
        ListarErroresRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.RegistrosError;

        // Las fechas son locales del instituto; "hasta" incluye su día completo.
        var (inicio, fin) = fechaLocal.RangoUtc(request.Desde, request.Hasta);
        if (inicio is { } desde)
        {
            query = query.Where(r => r.Fecha >= desde);
        }

        if (fin is { } hasta)
        {
            query = query.Where(r => r.Fecha < hasta);
        }

        if (request.Origen is { } origen)
        {
            query = query.Where(r => r.Origen == origen);
        }

        // El validador ya rechazó los formatos inválidos.
        if (ReglasErrores.NormalizarTraceId(request.TraceId) is { } traceId)
        {
            query = query.Where(r => r.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.Mensaje.ToLower().Contains(termino)
                || r.TipoExcepcion.ToLower().Contains(termino)
                || (r.Ruta != null && r.Ruta.ToLower().Contains(termino)));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return await query
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.Id)
            .Select(r => new RegistroErrorResponse(
                r.Id,
                r.Fecha,
                r.Origen,
                r.Descripcion,
                r.TipoExcepcion,
                r.Mensaje,
                r.Metodo,
                r.Ruta,
                r.UsuarioId,
                r.UsuarioRol,
                r.TraceId))
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>Un error con su detalle técnico. Uno ya depurado (RN-49) no existe.</summary>
    public async Task<RegistroErrorDetalleResponse> ObtenerAsync(long id, CancellationToken cancellationToken) =>
        await db.RegistrosError
            .Where(r => r.Id == id)
            .Select(r => new RegistroErrorDetalleResponse(
                r.Id,
                r.Fecha,
                r.Origen,
                r.Descripcion,
                r.TipoExcepcion,
                r.Mensaje,
                r.Metodo,
                r.Ruta,
                r.UsuarioId,
                r.UsuarioRol,
                r.TraceId,
                r.Detalle))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el error {id}.");
}
