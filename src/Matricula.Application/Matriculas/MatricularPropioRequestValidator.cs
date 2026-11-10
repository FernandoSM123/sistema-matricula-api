using FluentValidation;

namespace Matricula.Application.Matriculas;

public class MatricularPropioRequestValidator : AbstractValidator<MatricularPropioRequest>
{
    public MatricularPropioRequestValidator()
    {
        RuleFor(r => r.GrupoId)
            .NotNull().WithMessage("Indique el grupo.")
            .GreaterThan(0).WithMessage("El id del grupo debe ser mayor que 0.");
    }
}
