using FluentValidation;

namespace Matricula.Application.Aulas;

public class RegistrarAulaRequestValidator : AbstractValidator<RegistrarAulaRequest>
{
    public RegistrarAulaRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Ubicacion).Ubicacion();
        RuleFor(r => r.Capacidad).Capacidad();
    }
}
