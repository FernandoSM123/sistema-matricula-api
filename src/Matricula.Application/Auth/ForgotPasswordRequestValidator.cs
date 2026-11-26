using FluentValidation;

namespace Matricula.Application.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(r => r.Correo)
            .NotEmpty().WithMessage("Indique su correo.")
            .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");
    }
}