namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>Los tres periodos que siempre se siembran (supuesto 5 de la spec).</summary>
public sealed record PeriodoSamples(PeriodoSample Pasado, PeriodoSample Vigente, PeriodoSample Futuro);
