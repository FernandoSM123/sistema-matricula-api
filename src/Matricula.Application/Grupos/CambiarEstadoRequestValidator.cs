using FluentValidation;

namespace Matricula.Application.Grupos;

public class CambiarEstadoRequestValidator : AbstractValidator<CambiarEstadoRequest>
{
    public CambiarEstadoRequestValidator()
    {
        RuleFor(r => r.Estado)
            .NotNull().WithMessage("Indique el nuevo estado.")
            .IsInEnum().WithMessage("El estado debe ser Planificado, Abierto, Cerrado, EnCurso, Finalizado o Cancelado.");
    }
}
