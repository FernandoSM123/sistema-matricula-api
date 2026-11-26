using FluentValidation;

namespace Matricula.Application.Aulas;

public class CreateAulaRequestValidator : AbstractValidator<CreateAulaRequest>
{
    public CreateAulaRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Ubicacion).Ubicacion();
        RuleFor(r => r.Capacidad).Capacidad();
    }
}
