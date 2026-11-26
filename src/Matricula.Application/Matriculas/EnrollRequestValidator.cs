using FluentValidation;

namespace Matricula.Application.Matriculas;

public class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(r => r.EstudianteId)
            .NotNull().WithMessage("Indique el estudiante.")
            .GreaterThan(0).WithMessage("El id del estudiante debe ser mayor que 0.");

        RuleFor(r => r.GrupoId)
            .NotNull().WithMessage("Indique el grupo.")
            .GreaterThan(0).WithMessage("El id del grupo debe ser mayor que 0.");
    }
}
