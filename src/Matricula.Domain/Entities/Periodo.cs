using Matricula.Domain.Common;
using Matricula.Domain.Exceptions;
using Matricula.Domain.Reglas;

namespace Matricula.Domain.Entities;

/// <summary>
/// Periodo académico. No se desactiva (RF-37), así que no tiene estado.
/// </summary>
public class Periodo : EntidadBase
{
    // Para EF Core.
    private Periodo()
    {
        Nombre = string.Empty;
    }

    public Periodo(
        string nombre,
        DateOnly inicioClases,
        DateOnly finClases,
        DateOnly inicioMatricula,
        DateOnly finMatricula,
        DateOnly fechaLimiteRetiro)
    {
        Nombre = string.Empty;
        ActualizarDatos(nombre, inicioClases, finClases, inicioMatricula, finMatricula, fechaLimiteRetiro);
    }

    public string Nombre { get; private set; }
    public DateOnly InicioClases { get; private set; }
    public DateOnly FinClases { get; private set; }
    public DateOnly InicioMatricula { get; private set; }
    public DateOnly FinMatricula { get; private set; }
    public DateOnly FechaLimiteRetiro { get; private set; }

    /// <summary>
    /// Forma canónica del nombre: en mayúsculas para que RN-13 no distinga capitalización.
    /// </summary>
    public static string NormalizarNombre(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        return nombre.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Edición por un administrador (RF-36). El validador ya rechaza las fechas incoherentes con 400; aquí RN-16 se
    /// comprueba otra vez como invariante.
    /// </summary>
    public void ActualizarDatos(
        string nombre,
        DateOnly inicioClases,
        DateOnly finClases,
        DateOnly inicioMatricula,
        DateOnly finMatricula,
        DateOnly fechaLimiteRetiro)
    {
        var nombreNormalizado = NormalizarNombre(nombre);

        if (!ReglasPeriodo.Coherente(inicioClases, finClases, inicioMatricula, finMatricula, fechaLimiteRetiro))
        {
            throw new BusinessRuleException("RN-16", "Las fechas del periodo no son coherentes.");
        }

        Nombre = nombreNormalizado;
        InicioClases = inicioClases;
        FinClases = finClases;
        InicioMatricula = inicioMatricula;
        FinMatricula = finMatricula;
        FechaLimiteRetiro = fechaLimiteRetiro;
    }

    /// <summary>RN-04: la matrícula se permite desde su inicio hasta su fin, ambos días incluidos.</summary>
    public bool EnRangoMatricula(DateOnly fecha) => InicioMatricula <= fecha && fecha <= FinMatricula;

    /// <summary>RN-10: los retiros y cambios de grupo se permiten hasta la fecha límite, incluida.</summary>
    public bool PermiteRetiro(DateOnly fecha) => fecha <= FechaLimiteRetiro;
}
