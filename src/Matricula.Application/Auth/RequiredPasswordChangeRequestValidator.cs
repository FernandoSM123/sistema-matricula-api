using FluentValidation;

namespace Matricula.Application.Auth;

public class RequiredPasswordChangeRequestValidator : AbstractValidator<RequiredPasswordChangeRequest>
{
    public RequiredPasswordChangeRequestValidator()
    {
        RuleFor(r => r.NuevaContrasena).ValidNewPassword();
        RuleFor(r => r.Confirmacion).ConfirmationOf(r => r.NuevaContrasena);
    }
}