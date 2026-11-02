using FluentValidation;
using Matricula.Domain.Reglas;

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
            .Must((r, fin) => ReglasPeriodo.MatriculaOrdenada(r.InicioMatricula!.Value, fin!.Value))
            .WithMessage(ReglasPeriodo.MensajeMatriculaOrdenada)
            .When(r => r.InicioMatricula.HasValue && r.FinMatricula.HasValue);

        RuleFor(r => r.FinClases)
            .Must((r, fin) => ReglasPeriodo.ClasesOrdenadas(r.InicioClases!.Value, fin!.Value))
            .WithMessage(ReglasPeriodo.MensajeClasesOrdenadas)
            .When(r => r.InicioClases.HasValue && r.FinClases.HasValue);

        RuleFor(r => r.FinMatricula)
            .Must((r, fin) => ReglasPeriodo.MatriculaDentroDeClases(fin!.Value, r.FinClases!.Value))
            .WithMessage(ReglasPeriodo.MensajeMatriculaDentroDeClases)
            .When(r => r.FinMatricula.HasValue && r.FinClases.HasValue);

        RuleFor(r => r.FechaLimiteRetiro)
            .Must((r, limite) =>
                ReglasPeriodo.RetiroDentroDelPeriodo(limite!.Value, r.InicioMatricula!.Value, r.FinClases!.Value))
            .WithMessage(ReglasPeriodo.MensajeRetiroDentroDelPeriodo)
            .When(r => r.FechaLimiteRetiro.HasValue && r.InicioMatricula.HasValue && r.FinClases.HasValue);
    }
}
