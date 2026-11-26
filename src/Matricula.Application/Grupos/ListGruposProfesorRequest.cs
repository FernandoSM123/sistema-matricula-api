using Matricula.Application.Common.Pagination;

namespace Matricula.Application.Grupos;

/// <summary>
/// Grupos de un profesor (RF-27). <see cref="PageRequest.Search"/> busca en el NRC.
/// </summary>
public class ListGruposProfesorRequest : PageRequest
{
    public int? PeriodoId { get; set; }
}
