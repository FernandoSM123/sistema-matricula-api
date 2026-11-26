using FluentValidation;
using Matricula.Domain.Rules;

namespace Matricula.Application.Periodos;

/// <summary>
/// Reglas compartidas por el registro y la edición. Cada condición de RN-16 se revisa solo si llegaron sus fechas y
/// reporta el error en la fecha que la incumple.
/// </summary>
public abstract class DatosPeriodoValidator<T> : AbstractValidator<T>
    where T : IDatosPeriodo
{
    protected DatosPeriodoValidator()
    {
        RuleFor(r => r.Nombre)
            .NotEmpty().WithMessage("Ingrese el nombre del periodo.")
            .MaximumLength(50).WithMessage("El nombre no puede superar los 50 caracteres.");

        RuleFor(r => r.InicioClases).NotNull().WithMessage("Ingrese el inicio de clases.");
        RuleFor(r => r.FinClases).NotNull().WithMessage("Ingrese el fin de clases.");
        RuleFor(r => r.InicioMatricula).NotNull().WithMessage("Ingrese el inicio de la matrícula.");
        RuleFor(r => r.FinMatricula).NotNull().WithMessage("Ingrese el fin de la matrícula.");
        RuleFor(r => r.FechaLimiteRetiro).NotNull().WithMessage("Ingrese la fecha límite de retiro.");

        RuleFor(r => r.FinMatricula)
            .Must((r, fin) => PeriodoRules.MatriculaInOrder(r.InicioMatricula!.Value, fin!.Value))
            .WithMessage(PeriodoRules.MatriculaInOrderMessage)
            .When(r => r.InicioMatricula.HasValue && r.FinMatricula.HasValue);

        RuleFor(r => r.FinClases)
            .Must((r, fin) => PeriodoRules.ClasesInOrder(r.InicioClases!.Value, fin!.Value))
            .WithMessage(PeriodoRules.ClasesInOrderMessage)
            .When(r => r.InicioClases.HasValue && r.FinClases.HasValue);

        RuleFor(r => r.FinMatricula)
            .Must((r, fin) => PeriodoRules.MatriculaWithinClases(fin!.Value, r.FinClases!.Value))
            .WithMessage(PeriodoRules.MatriculaWithinClasesMessage)
            .When(r => r.FinMatricula.HasValue && r.FinClases.HasValue);

        RuleFor(r => r.FechaLimiteRetiro)
            .Must((r, limite) =>
                PeriodoRules.RetiroWithinPeriodo(limite!.Value, r.InicioMatricula!.Value, r.FinClases!.Value))
            .WithMessage(PeriodoRules.RetiroWithinPeriodoMessage)
            .When(r => r.FechaLimiteRetiro.HasValue && r.InicioMatricula.HasValue && r.FinClases.HasValue);
    }
}
