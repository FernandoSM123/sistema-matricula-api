using FluentValidation;

namespace Matricula.Application.Cursos;

public class ActualizarCursoRequestValidator : AbstractValidator<ActualizarCursoRequest>
{
    public ActualizarCursoRequestValidator()
    {
        RuleFor(r => r.Codigo).Codigo();
        RuleFor(r => r.Nombre).NombreCurso();
        RuleFor(r => r.Descripcion).Descripcion();
        RuleFor(r => r.DuracionHoras).DuracionHoras();
        RuleFor(r => r.Costo).Costo();
        RuleFor(r => r.Nivel).Nivel();
    }
}
