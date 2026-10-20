using Matricula.Application.Common.Exceptions;
using Matricula.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Matricula.Api.Common;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails con el status HTTP correspondiente.
/// </summary>
public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var violacionUnicidad = exception is DbUpdateException
        {
            InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        } ? postgres : null;

        var (status, title, code) = exception switch
        {
            BusinessRuleException e => (StatusCodes.Status409Conflict, "Regla de negocio incumplida", e.Code),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto", null),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Acceso denegado", e.Code),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autenticado", null),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia", "CONCURRENCIA"),
            _ when violacionUnicidad is not null => (StatusCodes.Status409Conflict, "Dato duplicado", "RN-13"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", (string?)null)
        };

        string detail;
        if (exception is DbUpdateConcurrencyException)
        {
            detail = "Los datos cambiaron mientras se procesaba la solicitud. Intente de nuevo.";
        }
        else if (violacionUnicidad is not null)
        {
            // Red de seguridad: los servicios validan antes los duplicados. El nombre de la restricción
            // solo va al log, para no exponer la estructura de la BD.
            detail = "Ya existe un registro con ese mismo valor (cédula, correo, código, NRC o nombre). Revise los datos.";
            logger.LogWarning("Violación de unicidad en la restricción {Restriccion}", violacionUnicidad.ConstraintName);
        }
        else if (status == StatusCodes.Status500InternalServerError)
        {
            detail = "Ocurrió un error inesperado.";
            logger.LogError(exception, "Error no controlado procesando {Path}", httpContext.Request.Path);
        }
        else
        {
            detail = exception.Message;
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        if (exception is BusinessRuleException { Datos: var datos })
        {
            foreach (var (clave, valor) in datos)
            {
                problem.Extensions[clave] = valor;
            }
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
