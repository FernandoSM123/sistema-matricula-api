using FluentValidation;
using Matricula.Domain.Entities;

namespace Matricula.Application.Grupos;

/// <summary>
/// Reglas compartidas por la creación y la edición. Las que necesitan la BD (RN-09, RN-13, RN-17) las valida el
/// servicio.
/// </summary>
public abstract class DatosGrupoValidator<T> : AbstractValidator<T>
    where T : IDatosGrupo
{
    protected DatosGrupoValidator()
    {
        RuleFor(r => r.Nrc)
            .NotEmpty().WithMessage("Ingrese el NRC del grupo.")
            .MaximumLength(10).WithMessage("El NRC no puede superar los 10 caracteres.");

        RuleFor(r => r.CursoId).NotNull().WithMessage("Seleccione el curso.");
        RuleFor(r => r.ProfesorId).NotNull().WithMessage("Seleccione el profesor.");
        RuleFor(r => r.PeriodoId).NotNull().WithMessage("Seleccione el periodo.");

        RuleFor(r => r.Cupo)
            .NotNull().WithMessage("Ingrese el cupo del grupo.")
            .GreaterThan(0).WithMessage("El cupo debe ser mayor que 0.");

        RuleFor(r => r.Modalidad)
            .NotNull().WithMessage("Seleccione la modalidad.")
            .IsInEnum().WithMessage("La modalidad debe ser Presencial, Virtual o Hibrida.");

        // RN-09: el aula depende de la modalidad.
        RuleFor(r => r.AulaId)
            .NotNull().WithMessage("Seleccione el aula: la modalidad presencial e híbrida se imparten en un aula.")
            .When(r => r.Modalidad.HasValue && Grupo.RequiereAula(r.Modalidad.Value));

        RuleFor(r => r.AulaId)
            .Null().WithMessage("Un grupo virtual no lleva aula.")
            .When(r => r.Modalidad.HasValue && !Grupo.RequiereAula(r.Modalidad.Value));
    }
}
