using Matricula.Application.Common.Paginacion;

namespace Matricula.Application.Profesores;

/// <summary>
/// <see cref="PaginacionRequest.Search"/> busca en nombre, apellidos, cédula, especialidad y correo.
/// </summary>
public class ListarProfesoresRequest : PaginacionRequest
{
    /// <summary>RN-41: por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }
}
