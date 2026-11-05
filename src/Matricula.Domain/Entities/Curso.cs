using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Curso del catálogo sobre el que se abren los grupos. Guarda su propio estado porque no tiene cuenta.
/// </summary>
public class Curso : EntidadBase
{
    // Para EF Core.
    private Curso()
    {
        Codigo = Nombre = string.Empty;
    }

    public Curso(
        string codigo,
        string nombre,
        string? descripcion,
        int duracionHoras,
        decimal costo,
        NivelCurso nivel)
    {
        Codigo = Nombre = string.Empty;
        ActualizarDatos(codigo, nombre, descripcion, duracionHoras, costo, nivel);
        Activo = true;
    }

    public string Codigo { get; private set; }
    public string Nombre { get; private set; }
    public string? Descripcion { get; private set; }
    public int DuracionHoras { get; private set; }
    public decimal Costo { get; private set; }
    public NivelCurso Nivel { get; private set; }
    public bool Activo { get; private set; }

    /// <summary>
    /// Forma canónica del código: en mayúsculas para que RN-13 no distinga capitalización.
    /// </summary>
    public static string NormalizarCodigo(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return codigo.Trim().ToUpperInvariant();
    }

    /// <summary>Edición por un administrador (RF-32).</summary>
    public void ActualizarDatos(
        string codigo,
        string nombre,
        string? descripcion,
        int duracionHoras,
        decimal costo,
        NivelCurso nivel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);

        Codigo = NormalizarCodigo(codigo);
        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        DuracionHoras = duracionHoras;
        Costo = costo;
        Nivel = nivel;
    }

    public void Desactivar() => Activo = false;

    public void Reactivar() => Activo = true;
}
