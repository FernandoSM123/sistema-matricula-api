using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Aulas;

public static class AulaProjections
{
    /// <summary>
    /// Para las consultas (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Aula, AulaResponse>> ToResponseExpression = a => new AulaResponse(
        a.Id,
        a.Codigo,
        a.Ubicacion,
        a.Capacidad,
        a.Activo,
        a.FechaCreacion);

    private static readonly Func<Aula, AulaResponse> ToResponseCompiled = ToResponseExpression.Compile();

    /// <summary>
    /// Para una entidad ya cargada.
    /// </summary>
    public static AulaResponse ToResponse(this Aula aula) => ToResponseCompiled(aula);
}
