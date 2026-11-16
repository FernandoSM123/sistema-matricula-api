namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Textos fijos de la siembra: Bogus no genera nombres de cursos plausibles para un instituto (punto 5 de la spec).
/// </summary>
public static class CatalogosPrueba
{
    /// <summary>Prefijo del código de curso de cada área.</summary>
    public static IReadOnlyList<(string Prefijo, string Nombre)> Cursos { get; } =
    [
        ("INF", "Excel básico"),
        ("INF", "Excel intermedio"),
        ("INF", "Excel avanzado y tablas dinámicas"),
        ("INF", "Introducción a la programación"),
        ("INF", "Programación web con JavaScript"),
        ("INF", "Bases de datos con SQL"),
        ("INF", "Python para análisis de datos"),
        ("INF", "Ofimática para oficinas"),
        ("INF", "Soporte técnico de computadoras"),
        ("IDI", "Inglés conversacional"),
        ("IDI", "Inglés para negocios"),
        ("IDI", "Portugués básico"),
        ("IDI", "Francés básico"),
        ("IDI", "Lesco básico"),
        ("ADM", "Servicio al cliente"),
        ("ADM", "Gestión de proyectos"),
        ("ADM", "Emprendimiento y plan de negocios"),
        ("ADM", "Recursos humanos"),
        ("CON", "Contabilidad básica"),
        ("CON", "Finanzas personales"),
        ("CON", "Facturación electrónica"),
        ("DIS", "Diseño gráfico con herramientas libres"),
        ("DIS", "Fotografía digital"),
        ("DIS", "Marketing digital y redes sociales"),
        ("DIS", "Edición de video"),
    ];

    public static IReadOnlyList<string> Especialidades { get; } =
    [
        "Informática",
        "Programación",
        "Bases de datos",
        "Inglés",
        "Idiomas",
        "Administración de empresas",
        "Contabilidad",
        "Finanzas",
        "Diseño gráfico",
        "Mercadeo",
    ];

    public static IReadOnlyList<(string Canton, string Provincia)> Cantones { get; } =
    [
        ("San José", "San José"),
        ("Desamparados", "San José"),
        ("Goicoechea", "San José"),
        ("Tibás", "San José"),
        ("Moravia", "San José"),
        ("Montes de Oca", "San José"),
        ("Curridabat", "San José"),
        ("Escazú", "San José"),
        ("Alajuela", "Alajuela"),
        ("San Ramón", "Alajuela"),
        ("Grecia", "Alajuela"),
        ("Cartago", "Cartago"),
        ("Paraíso", "Cartago"),
        ("La Unión", "Cartago"),
        ("Heredia", "Heredia"),
        ("Santo Domingo", "Heredia"),
        ("San Rafael", "Heredia"),
        ("Liberia", "Guanacaste"),
        ("Nicoya", "Guanacaste"),
        ("Puntarenas", "Puntarenas"),
        ("Esparza", "Puntarenas"),
        ("Limón", "Limón"),
        ("Pococí", "Limón"),
    ];

    public static IReadOnlyList<string> PuntosDeReferencia { get; } =
    [
        "de la iglesia católica",
        "de la escuela",
        "del parque central",
        "de la plaza de deportes",
        "del supermercado",
        "de la estación de buses",
        "del EBAIS",
        "de la municipalidad",
        "del colegio",
        "de la pulpería",
    ];

    /// <summary>Descripciones de curso; <c>{0}</c> es el nombre del curso, tal cual.</summary>
    public static IReadOnlyList<string> Descripciones { get; } =
    [
        "Curso práctico de {0} orientado al trabajo.",
        "Aprenda {0} con ejercicios y casos reales.",
        "Curso de {0} para principiantes y personas que desean actualizarse.",
        "Fundamentos y práctica guiada de {0}.",
        "Curso de {0} con proyecto final.",
    ];

    /// <summary>Sufijos para repetir cursos del catálogo cuando se piden más que los que tiene.</summary>
    public static IReadOnlyList<string> Sufijos { get; } = ["", " II", " III", " IV", " V", " VI", " VII", " VIII", " IX"];
}
