using FluentValidation;
using Matricula.Application.Common.Validacion;

namespace Matricula.Application.Administradores;

public class RegistrarAdministradorRequestValidator : AbstractValidator<RegistrarAdministradorRequest>
{
    public RegistrarAdministradorRequestValidator()
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
    }
}