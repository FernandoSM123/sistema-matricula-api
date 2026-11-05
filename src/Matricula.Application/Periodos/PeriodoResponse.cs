namespace Matricula.Application.Periodos;

public record PeriodoResponse(
    int Id,
    string Nombre,
    DateOnly InicioClases,
    DateOnly FinClases,
    DateOnly InicioMatricula,
    DateOnly FinMatricula,
    DateOnly FechaLimiteRetiro,
    DateTimeOffset FechaCreacion);
