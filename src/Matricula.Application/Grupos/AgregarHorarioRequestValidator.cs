using FluentValidation;
using Matricula.Domain.Reglas;

namespace Matricula.Application.Grupos;

public class AgregarHorarioRequestValidator : AbstractValidator<AgregarHorarioRequest>
{
    public AgregarHorarioRequestValidator()
    {
        RuleFor(r => r.Dia)
            .NotNull().WithMessage("Seleccione el día.")
            .IsInEnum().WithMessage("El día debe ser de Lunes a Domingo.");

        RuleFor(r => r.HoraInicio).NotNull().WithMessage("Ingrese la hora de inicio.");
        RuleFor(r => r.HoraFin).NotNull().WithMessage("Ingrese la hora de fin.");

        // RN-15
        RuleFor(r => r.HoraFin)
            .Must((r, fin) => ReglasHorario.HorasValidas(r.HoraInicio!.Value, fin!.Value))
            .WithMessage(ReglasHorario.MensajeHorasValidas)
            .When(r => r.HoraInicio.HasValue && r.HoraFin.HasValue);
    }
}
