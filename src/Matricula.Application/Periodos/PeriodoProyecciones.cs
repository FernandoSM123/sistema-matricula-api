using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Periodos;

public static class PeriodoProyecciones
{
    /// <summary>
    /// Para las consultas (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Periodo, PeriodoResponse>> AResponse = p => new PeriodoResponse(
        p.Id,
        p.Nombre,
        p.InicioClases,
        p.FinClases,
        p.InicioMatricula,
        p.FinMatricula,
        p.FechaLimiteRetiro,
        p.FechaCreacion);

    private static readonly Func<Periodo, PeriodoResponse> AResponseCompilada = AResponse.Compile();

    /// <summary>
    /// Para una entidad ya cargada.
    /// </summary>
    public static PeriodoResponse ToResponse(this Periodo periodo) => AResponseCompilada(periodo);
}
