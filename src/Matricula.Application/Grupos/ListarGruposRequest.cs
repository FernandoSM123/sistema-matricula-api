using Matricula.Application.Common.Pagination;
using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en el NRC. Para un profesor se ignora <see cref="ProfesorId"/> y para un
/// estudiante <see cref="Estado"/> (RN-18).
/// </summary>
public class ListarGruposRequest : PageRequest
{
    public int? CursoId { get; set; }
    public int? ProfesorId { get; set; }
    public int? PeriodoId { get; set; }
    public EstadoGrupo? Estado { get; set; }
    public Modalidad? Modalidad { get; set; }
}
