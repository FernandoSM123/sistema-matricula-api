namespace Matricula.Application.Common.Exceptions;

public class NotFoundException(string message = "El recurso solicitado no existe.") : Exception(message);
