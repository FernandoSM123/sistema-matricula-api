namespace Matricula.Application.Periodos;

public record CreatePeriodoRequest(
    string Nombre,
    DateOnly? InicioClases,
    DateOnly? FinClases,
    DateOnly? InicioMatricula,
    DateOnly? FinMatricula,
    DateOnly? FechaLimiteRetiro) : IPeriodoData;
