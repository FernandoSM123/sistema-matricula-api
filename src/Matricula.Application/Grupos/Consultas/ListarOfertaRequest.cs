using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos.Consultas;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en el nombre o código del curso y en el NRC.
/// </summary>
public class ListarOfertaRequest : PaginacionRequest
{
    public int? CursoId { get; set; }
    public Modalidad? Modalidad { get; set; }
}
