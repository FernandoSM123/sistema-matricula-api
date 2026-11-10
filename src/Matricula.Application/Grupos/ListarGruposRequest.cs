using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en el NRC. Para un profesor se ignora <see cref="ProfesorId"/> y para un
/// estudiante <see cref="Estado"/> (RN-18).
/// </summary>
public class ListarGruposRequest : PaginacionRequest
{
    public int? CursoId { get; set; }
    public int? ProfesorId { get; set; }
    public int? PeriodoId { get; set; }
    public EstadoGrupo? Estado { get; set; }
    public Modalidad? Modalidad { get; set; }
}
