using Matricula.Application.Common.Pagination;
using Matricula.Domain.Enums;

namespace Matricula.Application.Cursos;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en nombre y código.
/// </summary>
public class ListarCursosRequest : PageRequest
{
    public NivelCurso? Nivel { get; set; }

    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
