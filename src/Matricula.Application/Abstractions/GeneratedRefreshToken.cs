namespace Matricula.Application.Abstractions;

/// <summary>
/// Refresh token recién generado. <see cref="Token"/> solo viaja en la respuesta; en la BD se guarda <see cref="Hash"/>.
/// </summary>
public record GeneratedRefreshToken(string Token, string Hash);
