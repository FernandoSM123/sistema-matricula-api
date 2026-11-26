using Matricula.Application.Common.Pagination;

namespace Matricula.Application.Administradores;

/// <summary>
/// <see cref="PageRequest.Search"/> busca en nombre, apellidos, cédula y correo.
/// </summary>
public class ListarAdministradoresRequest : PageRequest
{
    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}