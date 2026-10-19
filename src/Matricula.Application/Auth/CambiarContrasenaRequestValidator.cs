using FluentValidation;

namespace Matricula.Application.Auth;

public class CambiarContrasenaRequestValidator : AbstractValidator<CambiarContrasenaRequest>
{
    public CambiarContrasenaRequestValidator()
    {
        RuleFor(r => r.ContrasenaActual)
            .NotEmpty().WithMessage("Indique su contraseña actual.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.");

        RuleFor(r => r.NuevaContrasena).NuevaContrasenaValida();
        RuleFor(r => r.Confirmacion).ConfirmacionDe(r => r.NuevaContrasena);
    }
}