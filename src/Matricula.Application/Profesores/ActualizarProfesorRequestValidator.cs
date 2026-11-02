using FluentValidation;
using Matricula.Application.Common.Validacion;

namespace Matricula.Application.Profesores;

public class ActualizarProfesorRequestValidator : AbstractValidator<ActualizarProfesorRequest>
{
    public ActualizarProfesorRequestValidator()
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
