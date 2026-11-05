using Matricula.Application.Abstractions;
using Matricula.Domain.Reglas;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Tiempo;

public class FechaLocal(TimeProvider timeProvider, IOptions<InstitutoOptions> options) : IFechaLocal
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(options.Value.ZonaHoraria);

    public DateOnly Hoy() => FechaInstituto.Calcular(timeProvider.GetUtcNow(), _zona);
}
