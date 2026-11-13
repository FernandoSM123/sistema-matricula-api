using FluentValidation;
using Matricula.Domain.Reglas;

namespace Matricula.Application.Errores;

public class ListarErroresRequestValidator : AbstractValidator<ListarErroresRequest>
{
    public ListarErroresRequestValidator()
    {
        RuleFor(r => r.Hasta)
            .Must((r, hasta) => r.Desde <= hasta)
            .WithMessage("La fecha hasta no puede ser anterior a la fecha desde.")
            .When(r => r.Desde.HasValue && r.Hasta.HasValue);

        RuleFor(r => r.TraceId)
            .Must(traceId => ReglasErrores.NormalizarTraceId(traceId) is not null)
            .WithMessage(
                "El identificador de operación no es válido. Use el traceId del error (00-<32 hex>-<16 hex>-<2 hex>) " +
                "o sus 32 caracteres hexadecimales.")
            .When(r => !string.IsNullOrWhiteSpace(r.TraceId));
    }
}
