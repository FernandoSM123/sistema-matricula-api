using FluentValidation;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Estudiantes;

public class UpdateContactoRequestValidator : AbstractValidator<UpdateContactoRequest>
{
    public UpdateContactoRequestValidator()
    {
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Direccion).Direccion();
    }
}
