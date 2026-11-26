using Matricula.Application.Common.Pagination;

namespace Matricula.Application.Profesores;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en nombre, apellidos, cédula, especialidad y correo.
/// </summary>
public class ListarProfesoresRequest : PageRequest
{
    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
