using FluentValidation;

namespace Matricula.Application.Aulas;

public class ActualizarAulaRequestValidator : AbstractValidator<ActualizarAulaRequest>
{
    public ActualizarAulaRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Ubicacion).Ubicacion();
        RuleFor(r => r.Capacidad).Capacidad();
    }
}
