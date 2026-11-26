using FluentValidation;

namespace Matricula.Application.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.ContrasenaActual)
            .NotEmpty().WithMessage("Indique su contraseña actual.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.");

        RuleFor(r => r.NuevaContrasena).ValidNewPassword();
        RuleFor(r => r.Confirmacion).ConfirmationOf(r => r.NuevaContrasena);
    }
}