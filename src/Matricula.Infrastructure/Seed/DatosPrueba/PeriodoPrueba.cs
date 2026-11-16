namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>Fechas y nombre de un periodo de prueba.</summary>
public sealed record PeriodoPrueba(
    string Nombre,
    DateOnly InicioClases,
    DateOnly FinClases,
    DateOnly InicioMatricula,
    DateOnly FinMatricula,
    DateOnly FechaLimiteRetiro);
