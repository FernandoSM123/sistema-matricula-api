using FluentValidation;
using Matricula.Application.Abstractions;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Estudiantes;

public class UpdateEstudianteRequestValidator : AbstractValidator<UpdateEstudianteRequest>
{
    public UpdateEstudianteRequestValidator(ILocalClock localClock)
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
        RuleFor(r => r.FechaNacimiento).FechaNacimiento(localClock);
        RuleFor(r => r.Direccion).Direccion();
    }
}
