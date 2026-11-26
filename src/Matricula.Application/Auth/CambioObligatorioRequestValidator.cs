using FluentValidation;

namespace Matricula.Application.Auth;

public class CambioObligatorioRequestValidator : AbstractValidator<CambioObligatorioRequest>
{
    public CambioObligatorioRequestValidator()
    {
        RuleFor(r => r.NuevaContrasena).ValidNewPassword();
        RuleFor(r => r.Confirmacion).ConfirmationOf(r => r.NuevaContrasena);
    }
}