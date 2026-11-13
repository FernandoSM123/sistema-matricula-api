using Matricula.Application.Errores;

namespace Matricula.Application.Abstractions;

/// <summary>
/// Registra un error inesperado (RF-58). Nunca lanza excepciones ni cambia el resultado de la operación (RN-48): si el
/// registro falla, el error queda solo en el log, que quien llama ya escribió antes.
/// </summary>
public interface IRegistroErrores
{
    // Sin CancellationToken: el de la petición puede venir cancelado y el registro no debe depender de él.
    Task RegistrarAsync(ErrorInesperado errorInesperado);
}
