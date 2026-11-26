using Matricula.Application.Common.Pagination;
using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos.Queries;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en el nombre o código del curso y en el NRC.
/// </summary>
public class ListarOfertaRequest : PageRequest
{
    public int? CursoId { get; set; }
    public Modalidad? Modalidad { get; set; }
}
