using FluentValidation;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Administradores;

public class ActualizarAdministradorRequestValidator : AbstractValidator<ActualizarAdministradorRequest>
{
    public ActualizarAdministradorRequestValidator()
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
    }
}