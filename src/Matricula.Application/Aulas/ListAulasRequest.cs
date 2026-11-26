using Matricula.Application.Common.Pagination;

namespace Matricula.Application.Aulas;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en código y ubicación.
/// </summary>
public class ListAulasRequest : PageRequest
{
    /// <summary>RN-41: por defecto solo se listan las activas.</summary>
    public bool IncluirInactivos { get; set; }
}
