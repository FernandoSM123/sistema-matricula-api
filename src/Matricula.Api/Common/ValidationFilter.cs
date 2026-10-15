using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Matricula.Api.Common;

/// <summary>
/// Ejecuta el validador de FluentValidation de cada argumento de la acción, si existe,
/// y responde 400 con ValidationProblemDetails cuando hay errores.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errores = new ModelStateDictionary();

        foreach (var argumento in context.ActionArguments.Values.OfType<object>())
        {
            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (serviceProvider.GetService(tipoValidador) is not IValidator validador)
            {
                continue;
            }

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);

            foreach (var error in resultado.Errors)
            {
                errores.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (errores.ErrorCount > 0)
        {
            // La fábrica aplica los valores por defecto de ProblemDetails, entre ellos el traceId.
            var problem = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext, errores, StatusCodes.Status400BadRequest, "Los datos enviados no son válidos");
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
