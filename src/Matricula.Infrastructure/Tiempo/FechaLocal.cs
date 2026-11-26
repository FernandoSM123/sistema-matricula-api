using Matricula.Application.Abstractions;
using Matricula.Domain.Rules;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Tiempo;

public class FechaLocal(TimeProvider timeProvider, IOptions<InstitutoOptions> options) : IFechaLocal
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(options.Value.ZonaHoraria);

    public DateOnly Hoy() => LocalDateRules.ToLocalDate(timeProvider.GetUtcNow(), _zona);

    public (DateTimeOffset? Inicio, DateTimeOffset? Fin) RangoUtc(DateOnly? desde, DateOnly? hasta) =>
        LocalDateRules.ToUtcRange(desde, hasta, _zona);
}
