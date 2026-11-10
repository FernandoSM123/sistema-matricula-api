using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Estudiantes de un grupo (RF-49, consulta 3). Por defecto los de matrícula activa; con Estado=Retirada, los que se
/// retiraron. <see cref="PaginacionRequest.Search"/> busca en el nombre, los apellidos y la cédula.
/// </summary>
public class ListarEstudiantesGrupoRequest : PaginacionRequest
{
    public EstadoMatricula Estado { get; set; } = EstadoMatricula.Activa;
}
