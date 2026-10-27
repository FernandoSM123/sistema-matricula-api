using Matricula.Application.Common.Paginacion;

namespace Matricula.Application.Estudiantes;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en nombre, apellidos, cédula y correo.
/// </summary>
public class ListarEstudiantesRequest : PaginacionRequest
{
    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
