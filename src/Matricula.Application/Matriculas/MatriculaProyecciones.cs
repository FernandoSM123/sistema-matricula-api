using System.Linq.Expressions;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Application.Matriculas;

public static class MatriculaProyecciones
{
    /// <summary>
    /// Para el detalle y los listados (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<MatriculaEntidad, MatriculaResponse>> AResponse = m => new MatriculaResponse(
        m.Id,
        m.EstudianteId,
        m.Estudiante.Cedula,
        m.Estudiante.Nombre + " " + m.Estudiante.Apellidos,
        m.GrupoId,
        m.Grupo.Nrc,
        m.Grupo.Modalidad,
        m.Grupo.Estado,
        m.Grupo.CursoId,
        m.Grupo.Curso.Codigo,
        m.Grupo.Curso.Nombre,
        m.Grupo.PeriodoId,
        m.Grupo.Periodo.Nombre,
        m.Grupo.Profesor.Nombre + " " + m.Grupo.Profesor.Apellidos,
        m.FechaMatricula,
        m.Estado,
        m.FechaRetiro,
        m.MotivoRetiro);
}
