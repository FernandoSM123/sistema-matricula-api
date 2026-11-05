using Matricula.Application.Common.Paginacion;

namespace Matricula.Application.Grupos;

/// <summary>
/// Grupos de un profesor (RF-27). <see cref="PaginacionRequest.Search"/> busca en el NRC.
/// </summary>
public class ListarGruposProfesorRequest : PaginacionRequest
{
    public int? PeriodoId { get; set; }
}
