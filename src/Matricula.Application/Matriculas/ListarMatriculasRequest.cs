using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Matrículas de un estudiante (RF-20, RF-54). Sin <see cref="Estado"/> es el historial completo (RF-54); con
/// Estado=Activa, los cursos y grupos matriculados (RF-20). <see cref="PaginacionRequest.Search"/> busca en el nombre
/// o código del curso y en el NRC.
/// </summary>
public class ListarMatriculasRequest : PaginacionRequest
{
    public int? PeriodoId { get; set; }
    public EstadoMatricula? Estado { get; set; }
}
