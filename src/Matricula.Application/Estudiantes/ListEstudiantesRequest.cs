using Matricula.Application.Common.Pagination;

namespace Matricula.Application.Estudiantes;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en nombre, apellidos, cédula y correo.
/// </summary>
public class ListEstudiantesRequest : PageRequest
{
    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
