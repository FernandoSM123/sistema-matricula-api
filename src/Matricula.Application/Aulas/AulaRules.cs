using FluentValidation;

namespace Matricula.Application.Aulas;

/// <summary>
/// Reglas de los datos de un aula, compartidas por el registro y la edición.
/// </summary>
public static class AulaRules
{
    public static void Codigo<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("Ingrese el código del aula.")
            .MaximumLength(20).WithMessage("El código no puede superar los 20 caracteres.");

    public static void Ubicacion<T>(this IRuleBuilder<T, string?> regla) =>
        regla.MaximumLength(150).WithMessage("La ubicación no puede superar los 150 caracteres.");

    public static void Capacidad<T>(this IRuleBuilder<T, int> regla) =>
        regla.GreaterThan(0).WithMessage("La capacidad debe ser mayor que 0.");
}
