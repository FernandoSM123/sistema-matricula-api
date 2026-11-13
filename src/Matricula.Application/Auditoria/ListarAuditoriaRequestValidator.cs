using FluentValidation;
using Matricula.Domain.Reglas;

namespace Matricula.Application.Auditoria;

public class ListarAuditoriaRequestValidator : AbstractValidator<ListarAuditoriaRequest>
{
    public ListarAuditoriaRequestValidator()
    {
        // Supuesto 7: un nombre desconocido es un error y no una lista vacía, que parecería "sin cambios".
        RuleFor(r => r.Entidad)
            .Must(entidad => ReglasAuditoria.BuscarEntidad(entidad) is not null)
            .WithMessage(
                $"La entidad no existe. Valores válidos: {string.Join(", ", ReglasAuditoria.EntidadesAuditadas)}.")
            .When(r => !string.IsNullOrWhiteSpace(r.Entidad));

        RuleFor(r => r.EntidadId)
            .GreaterThan(0).WithMessage("El id del registro debe ser mayor que 0.");
        RuleFor(r => r.EntidadId)
            .Null().WithMessage("Indique la entidad del registro: los ids se repiten entre entidades.")
            .When(r => string.IsNullOrWhiteSpace(r.Entidad));

        RuleFor(r => r.UsuarioId)
            .GreaterThan(0).WithMessage("El id del usuario debe ser mayor que 0.");

        RuleFor(r => r.Hasta)
            .Must((r, hasta) => r.Desde <= hasta)
            .WithMessage("La fecha hasta no puede ser anterior a la fecha desde.")
            .When(r => r.Desde.HasValue && r.Hasta.HasValue);

        RuleFor(r => r.OperacionId)
            .MaximumLength(32).WithMessage("El identificador de operación tiene 32 caracteres.");
    }
}