using FluentValidation;
using Matricula.Domain.Enums;

namespace Matricula.Application.Cursos;

/// <summary>
/// Reglas de los datos de un curso, compartidas por el registro y la edición.
/// </summary>
public static class CursoRules
{
    public static void Codigo<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("Ingrese el código del curso.")
            .MaximumLength(20).WithMessage("El código no puede superar los 20 caracteres.");

    public static void NombreCurso<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("Ingrese el nombre del curso.")
            .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres.");

    public static void Descripcion<T>(this IRuleBuilder<T, string?> regla) =>
        regla.MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres.");

    public static void DuracionHoras<T>(this IRuleBuilder<T, int> regla) =>
        regla.GreaterThan(0).WithMessage("La duración debe ser mayor que 0 horas.");

    // numeric(10,2): hasta 8 dígitos enteros y 2 decimales.
    public static void Costo<T>(this IRuleBuilder<T, decimal?> regla) =>
        regla
            .NotNull().WithMessage("Ingrese el costo del curso.")
            .GreaterThanOrEqualTo(0m).WithMessage("El costo no puede ser negativo.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage("El costo admite hasta 8 dígitos enteros y 2 decimales.");

    public static void Nivel<T>(this IRuleBuilder<T, NivelCurso?> regla) =>
        regla
            .NotNull().WithMessage("Seleccione el nivel del curso.")
            .IsInEnum().WithMessage("El nivel debe ser Basico, Intermedio o Avanzado.");
}
