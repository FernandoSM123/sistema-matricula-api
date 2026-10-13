namespace Matricula.Application.Common.Exceptions;

public class ConflictException(string message = "La operación entra en conflicto con el estado actual de los datos.") : Exception(message);
