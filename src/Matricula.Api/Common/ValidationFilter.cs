using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Matricula.Api.Common;

/// <summary>
/// Ejecuta el validador de FluentValidation de cada argumento de la acción, si existe,
/// y responde 400 con ValidationProblemDetails cuando hay errores.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errores = new Dictionary<string, string[]>();

        foreach (var argumento in context.ActionArguments.Values.OfType<object>())
        {
            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (serviceProvider.GetService(tipoValidador) is not IValidator validador)
            {
                continue;
            }

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);

            foreach (var grupo in resultado.Errors.GroupBy(e => e.PropertyName))
            {
                errores[grupo.Key] = grupo.Select(e => e.ErrorMessage).ToArray();
            }
        }

        if (errores.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errores)
            {
                Title = "Los datos enviados no son válidos",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        await next();
    }
}
