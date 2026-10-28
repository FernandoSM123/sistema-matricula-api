using FluentValidation;
using Matricula.Application.Common.Validacion;

namespace Matricula.Application.Estudiantes;

public class ActualizarContactoRequestValidator : AbstractValidator<ActualizarContactoRequest>
{
    public ActualizarContactoRequestValidator()
    {
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Direccion).Direccion();
    }
}
