namespace Matricula.Application.Periodos;

public record ActualizarPeriodoRequest(
    string Nombre,
    DateOnly? InicioClases,
    DateOnly? FinClases,
    DateOnly? InicioMatricula,
    DateOnly? FinMatricula,
    DateOnly? FechaLimiteRetiro) : IDatosPeriodo;
