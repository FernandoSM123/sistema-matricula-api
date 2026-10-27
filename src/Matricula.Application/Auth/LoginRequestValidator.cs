using FluentValidation;

namespace Matricula.Application.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Correo)
            .NotEmpty().WithMessage("Indique su correo.")
            .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        RuleFor(r => r.Contrasena)
            .NotEmpty().WithMessage("Indique su contraseña.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.");
    }
}