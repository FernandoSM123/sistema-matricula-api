namespace Matricula.Application.Common.Exceptions;

public class ForbiddenException(string message = "No tiene permiso para realizar esta operación.") : Exception(message);
