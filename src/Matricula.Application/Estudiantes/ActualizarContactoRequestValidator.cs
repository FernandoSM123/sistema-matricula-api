using FluentValidation;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Estudiantes;

public class ActualizarContactoRequestValidator : AbstractValidator<ActualizarContactoRequest>
{
    public ActualizarContactoRequestValidator()
    {
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Direccion).Direccion();
    }
}
