using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Periodos;

public static class PeriodoProjections
{
    /// <summary>
    /// Para las consultas (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Periodo, PeriodoResponse>> ToResponseExpression = p => new PeriodoResponse(
        p.Id,
        p.Nombre,
        p.InicioClases,
        p.FinClases,
        p.InicioMatricula,
        p.FinMatricula,
        p.FechaLimiteRetiro,
        p.FechaCreacion);

    private static readonly Func<Periodo, PeriodoResponse> ToResponseCompiled = ToResponseExpression.Compile();

    /// <summary>
    /// Para una entidad ya cargada.
    /// </summary>
    public static PeriodoResponse ToResponse(this Periodo periodo) => ToResponseCompiled(periodo);
}
