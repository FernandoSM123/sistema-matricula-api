using FluentValidation;

namespace Matricula.Application.Matriculas;

public class EnrollMeRequestValidator : AbstractValidator<EnrollMeRequest>
{
    public EnrollMeRequestValidator()
    {
        RuleFor(r => r.GrupoId)
            .NotNull().WithMessage("Indique el grupo.")
            .GreaterThan(0).WithMessage("El id del grupo debe ser mayor que 0.");
    }
}
