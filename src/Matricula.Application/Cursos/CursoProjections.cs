using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Cursos;

public static class CursoProjections
{
    /// <summary>
    /// Para las consultas (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Curso, CursoResponse>> ToResponseExpression = c => new CursoResponse(
        c.Id,
        c.Codigo,
        c.Nombre,
        c.Descripcion,
        c.DuracionHoras,
        c.Costo,
        c.Nivel,
        c.Activo,
        c.FechaCreacion);

    private static readonly Func<Curso, CursoResponse> ToResponseCompiled = ToResponseExpression.Compile();

    /// <summary>
    /// Para una entidad ya cargada.
    /// </summary>
    public static CursoResponse ToResponse(this Curso curso) => ToResponseCompiled(curso);
}
