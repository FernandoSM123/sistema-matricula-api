namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>Los tres periodos que siempre se siembran (supuesto 5 de la spec).</summary>
public sealed record PeriodosPrueba(PeriodoPrueba Pasado, PeriodoPrueba Vigente, PeriodoPrueba Futuro);
