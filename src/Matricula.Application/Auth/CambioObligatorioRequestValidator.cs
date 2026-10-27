using FluentValidation;

namespace Matricula.Application.Auth;

public class CambioObligatorioRequestValidator : AbstractValidator<CambioObligatorioRequest>
{
    public CambioObligatorioRequestValidator()
    {
        RuleFor(r => r.NuevaContrasena).NuevaContrasenaValida();
        RuleFor(r => r.Confirmacion).ConfirmacionDe(r => r.NuevaContrasena);
    }
}