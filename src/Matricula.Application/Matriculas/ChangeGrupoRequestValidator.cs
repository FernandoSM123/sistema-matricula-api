using FluentValidation;

namespace Matricula.Application.Matriculas;

public class ChangeGrupoRequestValidator : AbstractValidator<ChangeGrupoRequest>
{
    public ChangeGrupoRequestValidator()
    {
        RuleFor(r => r.GrupoDestinoId)
            .NotNull().WithMessage("Indique el grupo destino.")
            .GreaterThan(0).WithMessage("El id del grupo destino debe ser mayor que 0.");
    }
}
