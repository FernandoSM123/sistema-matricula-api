namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>Fechas y nombre de un periodo de prueba.</summary>
public sealed record PeriodoSample(
    string Nombre,
    DateOnly InicioClases,
    DateOnly FinClases,
    DateOnly InicioMatricula,
    DateOnly FinMatricula,
    DateOnly FechaLimiteRetiro);
