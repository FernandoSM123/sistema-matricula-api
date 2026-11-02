using FluentValidation;
using Matricula.Application.Abstractions;
using Matricula.Application.Common.Validacion;

namespace Matricula.Application.Estudiantes;

public class ActualizarEstudianteRequestValidator : AbstractValidator<ActualizarEstudianteRequest>
{
    public ActualizarEstudianteRequestValidator(IFechaLocal fechaLocal)
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
        RuleFor(r => r.FechaNacimiento).FechaNacimiento(fechaLocal);
        RuleFor(r => r.Direccion).Direccion();
    }
}
