using System.Linq.Expressions;
using FluentValidation;
using Matricula.Domain.Rules;

namespace Matricula.Application.Auth;

/// <summary>
/// Reglas compartidas por el cambio obligatorio y el cambio propio de contraseña.
/// </summary>
public static class NewPasswordRules
{
    /// <summary>RN-26.</summary>
    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Indique la nueva contraseña.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.")
            .Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Message);

    /// <summary>RN-25: la confirmación debe coincidir con la nueva contraseña.</summary>
    public static IRuleBuilderOptions<T, string> ConfirmationOf<T>(this IRuleBuilder<T, string> rule, Expression<Func<T, string>> nuevaContrasena) =>
        rule
            .NotEmpty().WithMessage("Confirme la nueva contraseña.")
            .Equal(nuevaContrasena).WithMessage("La confirmación no coincide con la nueva contraseña.");
}