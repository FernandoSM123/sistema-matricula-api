using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Grupos de un curso (RF-34). <see cref="PaginacionRequest.Search"/> busca en el NRC. Para quien no es administrador
/// se ignora <see cref="Estado"/>: solo ve los Abierto (supuesto 7).
/// </summary>
public class ListarGruposCursoRequest : PaginacionRequest
{
    public int? PeriodoId { get; set; }
    public EstadoGrupo? Estado { get; set; }
}
