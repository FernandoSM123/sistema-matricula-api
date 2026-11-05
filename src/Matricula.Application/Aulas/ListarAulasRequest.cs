using Matricula.Application.Common.Paginacion;

namespace Matricula.Application.Aulas;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en código y ubicación.
/// </summary>
public class ListarAulasRequest : PaginacionRequest
{
    /// <summary>RN-41: por defecto solo se listan las activas.</summary>
    public bool IncluirInactivos { get; set; }
}
