using FluentValidation;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Profesores;

public class CreateProfesorRequestValidator : AbstractValidator<CreateProfesorRequest>
{
    public CreateProfesorRequestValidator()
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
        RuleFor(r => r.Especialidad).Especialidad();
        RuleFor(r => r.FechaContratacion).FechaContratacion();
    }
}
