using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Errores;

/// <summary>
/// Filtros de los errores (RF-59), todos opcionales y combinables. <see cref="PaginacionRequest.Search"/> busca en el
/// mensaje, el tipo de la excepción y la ruta (supuesto 7 de la spec).
/// </summary>
public class ListarErroresRequest : PaginacionRequest
{
    /// <summary>Fecha local del instituto, incluida.</summary>
    public DateOnly? Desde { get; set; }

    /// <summary>Fecha local del instituto, incluida.</summary>
    public DateOnly? Hasta { get; set; }

    public OrigenError? Origen { get; set; }

    /// <summary>
    /// El traceId completo que recibe el usuario en el error (00-…-…-01) o solo sus 32 caracteres.
    /// </summary>
    public string? TraceId { get; set; }
}
