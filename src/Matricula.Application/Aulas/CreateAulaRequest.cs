namespace Matricula.Application.Aulas;

public record CreateAulaRequest(string Codigo, string? Ubicacion, int Capacidad);
