using FluentValidation;
using Matricula.Application.Auth;
using Matricula.Application.Common.Validacion;

namespace Matricula.Application.Estudiantes;

public class RegistroPublicoRequestValidator : AbstractValidator<RegistroPublicoRequest>
{
    public RegistroPublicoRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(r => r.Cedula).Cedula();
        RuleFor(r => r.Nombre).NombrePersona("el nombre");
        RuleFor(r => r.Apellidos).NombrePersona("los apellidos");
        RuleFor(r => r.Telefono).Telefono();
        RuleFor(r => r.Correo).Correo();
        RuleFor(r => r.FechaNacimiento).FechaNacimiento(timeProvider);
        RuleFor(r => r.Direccion).Direccion();
        RuleFor(r => r.Contrasena).NuevaContrasenaValida();
        RuleFor(r => r.Confirmacion).ConfirmacionDe(r => r.Contrasena);
    }
}
