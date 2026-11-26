using FluentValidation;

namespace Matricula.Application.Aulas;

public class UpdateAulaRequestValidator : AbstractValidator<UpdateAulaRequest>
{
    public UpdateAulaRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Ubicacion).Ubicacion();
        RuleFor(r => r.Capacidad).Capacidad();
    }
}
