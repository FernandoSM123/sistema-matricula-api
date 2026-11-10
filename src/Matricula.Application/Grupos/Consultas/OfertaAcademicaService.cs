using Matricula.Application.Abstractions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Periodos;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Grupos.Consultas;

public class OfertaAcademicaService(IAppDbContext db, PeriodoService periodoService)
{
    /// <summary>
    /// RF-50 (consulta 9): los grupos Abierto del periodo vigente, incluidos los que no tienen cupos disponibles. Sin
    /// periodo vigente devuelve una página vacía (supuesto 8). Ordena por curso y NRC.
    /// </summary>
    public async Task<PagedResult<OfertaAcademicaResponse>> ListarAsync(
        ListarOfertaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await periodoService.ObtenerIdVigenteAsync(cancellationToken) is not { } periodoId)
        {
            return new PagedResult<OfertaAcademicaResponse>([], request.Page, request.PageSize, 0);
        }

        var query = db.Grupos.AsNoTracking()
            .Where(g => g.PeriodoId == periodoId && g.Estado == EstadoGrupo.Abierto);

        if (request.CursoId is { } cursoId)
        {
            query = query.Where(g => g.CursoId == cursoId);
        }

        if (request.Modalidad is { } modalidad)
        {
            query = query.Where(g => g.Modalidad == modalidad);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(g => g.Curso.Nombre.ToLower().Contains(termino)
                || g.Curso.Codigo.ToLower().Contains(termino)
                || g.Nrc.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        var pagina = await query
            .OrderBy(g => g.Curso.Nombre)
            .ThenBy(g => g.Nrc)
            .ThenBy(g => g.Id)
            .Select(g => new OfertaAcademicaResponse(
                g.Id,
                g.Nrc,
                g.CursoId,
                g.Curso.Codigo,
                g.Curso.Nombre,
                g.Profesor.Nombre + " " + g.Profesor.Apellidos,
                g.Modalidad,
                g.Aula != null ? g.Aula.Codigo : null,
                g.Cupo,
                g.Cupo - g.CuposOcupados,
                g.Horarios.Select(h => new HorarioResponse(h.Id, h.Dia, h.HoraInicio, h.HoraFin)).ToList()))
            .ToPagedResultAsync(request, cancellationToken);

        // El día se guarda como texto: los bloques se ordenan en memoria, como en el detalle.
        return pagina with
        {
            Items = [.. pagina.Items.Select(o => o with
            {
                Horarios = [.. o.Horarios.OrderBy(h => h.Dia).ThenBy(h => h.HoraInicio)]
            })]
        };
    }
}
