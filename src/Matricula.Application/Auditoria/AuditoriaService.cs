using System.Text.Json;
using Matricula.Application.Abstractions;
using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Reglas;

namespace Matricula.Application.Auditoria;

/// <summary>
/// Consulta de la auditoría (RF-57). Solo lee: los registros los escribe AppDbContext al guardar (RN-43).
/// </summary>
public class AuditoriaService(IAppDbContext db, IFechaLocal fechaLocal)
{
    /// <summary>
    /// Filtra con todos los criterios recibidos y ordena del cambio más reciente al más antiguo.
    /// </summary>
    public async Task<PagedResult<RegistroAuditoriaResponse>> ListarAsync(
        ListarAuditoriaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.RegistrosAuditoria;

        // El validador ya rechazó los nombres desconocidos; aquí se pasa al nombre exacto para usar el índice.
        if (ReglasAuditoria.BuscarEntidad(request.Entidad) is { } entidad)
        {
            query = query.Where(r => r.Entidad == entidad);
        }

        if (request.EntidadId is { } entidadId)
        {
            query = query.Where(r => r.EntidadId == entidadId);
        }

        if (request.UsuarioId is { } usuarioId)
        {
            query = query.Where(r => r.UsuarioId == usuarioId);
        }

        if (request.Accion is { } accion)
        {
            query = query.Where(r => r.Accion == accion);
        }

        // Supuesto 8: las fechas son locales del instituto; "hasta" incluye su día completo.
        var (inicio, fin) = fechaLocal.RangoUtc(request.Desde, request.Hasta);
        if (inicio is { } desde)
        {
            query = query.Where(r => r.Fecha >= desde);
        }

        if (fin is { } hasta)
        {
            query = query.Where(r => r.Fecha < hasta);
        }

        if (!string.IsNullOrWhiteSpace(request.OperacionId))
        {
#pragma warning disable CA1308 // El trace id se guarda en hexadecimal en minúsculas.
            var operacionId = request.OperacionId.Trim().ToLowerInvariant();
#pragma warning restore CA1308
            query = query.Where(r => r.OperacionId == operacionId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(r => r.UsuarioCorreo != null && r.UsuarioCorreo.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        // Cambios llega como texto y se convierte a objeto JSON después de paginar, ya en memoria.
        var pagina = await query
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Fecha,
                r.Accion,
                r.Entidad,
                r.EntidadId,
                r.UsuarioId,
                r.UsuarioCorreo,
                r.UsuarioRol,
                r.OperacionId,
                r.Cambios
            })
            .ToPagedResultAsync(request, cancellationToken);

        var items = pagina.Items
            .Select(r => new RegistroAuditoriaResponse(
                r.Id,
                r.Fecha,
                r.Accion,
                r.Entidad,
                r.EntidadId,
                r.UsuarioId,
                r.UsuarioCorreo,
                r.UsuarioRol,
                r.OperacionId,
                LeerJson(r.Cambios)))
            .ToList();

        return new PagedResult<RegistroAuditoriaResponse>(items, pagina.Page, pagina.PageSize, pagina.TotalCount);
    }

    private static JsonElement LeerJson(string json)
    {
        using var documento = JsonDocument.Parse(json);
        return documento.RootElement.Clone();
    }
}