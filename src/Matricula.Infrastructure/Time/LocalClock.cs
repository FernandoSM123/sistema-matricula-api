using Matricula.Application.Abstractions;
using Matricula.Domain.Rules;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Time;

public class LocalClock(TimeProvider timeProvider, IOptions<InstituteOptions> options) : ILocalClock
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today() => LocalDateRules.ToLocalDate(timeProvider.GetUtcNow(), _zona);

    public (DateTimeOffset? Inicio, DateTimeOffset? Fin) ToUtcRange(DateOnly? desde, DateOnly? hasta) =>
        LocalDateRules.ToUtcRange(desde, hasta, _zona);
}
