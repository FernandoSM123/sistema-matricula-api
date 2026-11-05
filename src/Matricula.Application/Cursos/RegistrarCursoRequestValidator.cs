using FluentValidation;

namespace Matricula.Application.Cursos;

public class RegistrarCursoRequestValidator : AbstractValidator<RegistrarCursoRequest>
{
    public RegistrarCursoRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Nombre).NombreCurso();
        RuleFor(r => r.Descripcion).Descripcion();
        RuleFor(r => r.DuracionHoras).DuracionHoras();
        RuleFor(r => r.Costo).Costo();
        RuleFor(r => r.Nivel).Nivel();
    }
}
