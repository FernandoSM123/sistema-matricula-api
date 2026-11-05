using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Cursos;

public static class CursoProyecciones
{
    /// <summary>
    /// Para las consultas (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Curso, CursoResponse>> AResponse = c => new CursoResponse(
        c.Id,
        c.Codigo,
        c.Nombre,
        c.Descripcion,
        c.DuracionHoras,
        c.Costo,
        c.Nivel,
        c.Activo,
        c.FechaCreacion);

    private static readonly Func<Curso, CursoResponse> AResponseCompilada = AResponse.Compile();

    /// <summary>
    /// Para una entidad ya cargada.
    /// </summary>
    public static CursoResponse ToResponse(this Curso curso) => AResponseCompilada(curso);
}
