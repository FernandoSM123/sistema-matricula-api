using FluentValidation;

namespace Matricula.Application.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty().WithMessage("Indique el refresh token.")
            .MaximumLength(100).WithMessage("El refresh token no puede superar los 100 caracteres.");
    }
}
