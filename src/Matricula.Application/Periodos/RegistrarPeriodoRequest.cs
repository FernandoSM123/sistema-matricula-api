namespace Matricula.Application.Periodos;

public record RegistrarPeriodoRequest(
    string Nombre,
    DateOnly? InicioClases,
    DateOnly? FinClases,
    DateOnly? InicioMatricula,
    DateOnly? FinMatricula,
    DateOnly? FechaLimiteRetiro) : IDatosPeriodo;
