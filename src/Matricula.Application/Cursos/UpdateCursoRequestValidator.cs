using FluentValidation;

namespace Matricula.Application.Cursos;

public class UpdateCursoRequestValidator : AbstractValidator<UpdateCursoRequest>
{
    public UpdateCursoRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Nombre).NombreCurso();
        RuleFor(r => r.Descripcion).Descripcion();
        RuleFor(r => r.DuracionHoras).DuracionHoras();
        RuleFor(r => r.Costo).Costo();
        RuleFor(r => r.Nivel).Nivel();
    }
}
