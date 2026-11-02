using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Cursos;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en nombre y código.
/// </summary>
public class ListarCursosRequest : PaginacionRequest
{
    public NivelCurso? Nivel { get; set; }

    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
