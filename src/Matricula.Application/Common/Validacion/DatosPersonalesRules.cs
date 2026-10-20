using FluentValidation;

namespace Matricula.Application.Common.Validacion;

/// <summary>
/// Formato de los datos personales, compartido por administradores, estudiantes y profesores
/// (supuesto 6 de specs/SPEC-acceso.md).
/// </summary>
public static class DatosPersonalesRules
{
    /// <summary>Admite cédulas nacionales, de residencia y pasaportes.</summary>
    public static IRuleBuilderOptions<T, string> Cedula<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Indique la cédula.")
            .MaximumLength(20).WithMessage("La cédula no puede superar los 20 caracteres.")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("La cédula solo admite letras, números y guiones.");

    public static IRuleBuilderOptions<T, string> NombrePersona<T>(this IRuleBuilder<T, string> rule, string campo) =>
        rule
            .NotEmpty().WithMessage($"Indique {campo}.")
            .MaximumLength(100).WithMessage($"El campo {campo} no puede superar los 100 caracteres.");

    public static IRuleBuilderOptions<T, string> Telefono<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Indique el teléfono.")
            .Length(8, 20).WithMessage("El teléfono debe tener entre 8 y 20 caracteres.")
            .Matches(@"^[0-9 +\-]+$").WithMessage("El teléfono solo admite números, espacios, + y -.");

    public static IRuleBuilderOptions<T, string> Correo<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Indique el correo.")
            .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");
}