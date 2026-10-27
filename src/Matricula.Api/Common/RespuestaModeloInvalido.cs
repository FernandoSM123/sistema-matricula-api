using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Matricula.Api.Common;

/// <summary>
/// 400 cuando el cuerpo falta o no es un JSON válido. Los mensajes del framework están en inglés y
/// pueden describir tipos internos, así que se reemplazan por textos en español.
/// </summary>
public static class RespuestaModeloInvalido
{
    public static IActionResult Crear(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errores = new ModelStateDictionary();
        foreach (var (clave, entrada) in context.ModelState)
        {
            if (entrada.Errors.Count == 0)
            {
                continue;
            }

            var esElCuerpo = string.IsNullOrEmpty(clave) || clave == "$";
            errores.AddModelError(clave, esElCuerpo
                ? "Falta el cuerpo de la solicitud o no es un JSON válido."
                : "El valor enviado no tiene el formato esperado.");
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext, errores, StatusCodes.Status400BadRequest, "Los datos enviados no son válidos");

        return new BadRequestObjectResult(problem);
    }
}