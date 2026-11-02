using Matricula.Domain.Common;

namespace Matricula.Domain.Entities;

/// <summary>
/// Aula del catálogo donde se imparten los grupos. Guarda su propio estado porque no tiene cuenta.
/// </summary>
public class Aula : EntidadBase
{
    // Para EF Core.
    private Aula()
    {
        Codigo = string.Empty;
    }

    public Aula(string codigo, string? ubicacion, int capacidad)
    {
        Codigo = string.Empty;
        ActualizarDatos(codigo, ubicacion, capacidad);
        Activo = true;
    }

    public string Codigo { get; private set; }
    public string? Ubicacion { get; private set; }
    public int Capacidad { get; private set; }
    public bool Activo { get; private set; }

    /// <summary>
    /// Forma canónica del código: en mayúsculas para que RN-13 no distinga capitalización.
    /// </summary>
    public static string NormalizarCodigo(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return codigo.Trim().ToUpperInvariant();
    }

    /// <summary>Edición por un administrador (RF-40).</summary>
    public void ActualizarDatos(string codigo, string? ubicacion, int capacidad)
    {
        Codigo = NormalizarCodigo(codigo);
        Ubicacion = string.IsNullOrWhiteSpace(ubicacion) ? null : ubicacion.Trim();
        Capacidad = capacidad;
    }

    public void Desactivar() => Activo = false;

    public void Reactivar() => Activo = true;
}
