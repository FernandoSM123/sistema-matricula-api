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

        // Red de seguridad de RN-34: un registro que ganó datos relacionados entre la validación y el borrado. Las FK
        // son Restrict, así que la BD rechaza el DELETE.
        var violacionReferencia = exception is DbUpdateException
        {
            InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } referencia
        } ? referencia : null;

        var (status, title, code) = exception switch
        {
            BusinessRuleException e => (StatusCodes.Status409Conflict, "Regla de negocio incumplida", e.Code),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto", null),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Acceso denegado", e.Code),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autenticado", null),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia", "CONCURRENCIA"),
            _ when violacionUnicidad is not null => (StatusCodes.Status409Conflict, "Dato duplicado", "RN-13"),
            _ when violacionReferencia is not null => (StatusCodes.Status409Conflict, "Registro en uso", "RN-34"),
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
        else if (violacionReferencia is not null)
        {
            detail = "El registro tiene datos relacionados y no se puede eliminar. Desactívelo en su lugar.";
            logger.LogWarning("Violación de llave foránea en la restricción {Restriccion}", violacionReferencia.ConstraintName);
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
