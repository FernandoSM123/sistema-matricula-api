using System.Linq.Expressions;
using FluentValidation;
using Matricula.Domain.Reglas;

namespace Matricula.Application.Auth;

/// <summary>
/// Reglas compartidas por el cambio obligatorio y el cambio propio de contraseña.
/// </summary>
public static class NuevaContrasenaRules
{
    /// <summary>RN-26.</summary>
    public static IRuleBuilderOptions<T, string> NuevaContrasenaValida<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Indique la nueva contraseña.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.")
            .Must(PoliticaContrasena.Cumple).WithMessage(PoliticaContrasena.Mensaje);

    /// <summary>RN-25: la confirmación debe coincidir con la nueva contraseña.</summary>
    public static IRuleBuilderOptions<T, string> ConfirmacionDe<T>(this IRuleBuilder<T, string> rule, Expression<Func<T, string>> nuevaContrasena) =>
        rule
            .NotEmpty().WithMessage("Confirme la nueva contraseña.")
            .Equal(nuevaContrasena).WithMessage("La confirmación no coincide con la nueva contraseña.");
}