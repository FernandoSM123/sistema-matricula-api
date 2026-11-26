using FluentValidation;
using Matricula.Application.Abstractions;
using Matricula.Application.Auth;
using Matricula.Application.Common.Validation;

namespace Matricula.Application.Estudiantes;

public class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator(ILocalClock localClock)
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
        RuleFor(r => r.FechaNacimiento).FechaNacimiento(localClock);
        RuleFor(r => r.Direccion).Direccion();
        RuleFor(r => r.Contrasena).ValidNewPassword();
        RuleFor(r => r.Confirmacion).ConfirmationOf(r => r.Contrasena);
    }
}
