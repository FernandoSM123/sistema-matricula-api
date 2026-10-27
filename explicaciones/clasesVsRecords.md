# `record` vs `class` en C#

Las dos sirven para definir tipos, pero tienen propósitos distintos:

- Un **`record`** está pensado para **representar datos**.
- Una **`class`** está pensada para **objetos con comportamiento o identidad**.

Tu carpeta `Administradores` usa cada una justo para eso.

---

## 1. Qué hay en tu carpeta y por qué

| Archivo | Tipo | Por qué |
|---|---|---|
| `RegistrarAdministradorRequest` | `record` | Datos que llegan del cliente; no se modifican |
| `ActualizarAdministradorRequest` | `record` | Igual |
| `AdministradorResponse` | `record` | Datos que se devuelven; no se modifican |
| `AdministradorRegistradoResponse` | `record` | Igual |
| `ListarAdministradoresRequest` | `class` | Hereda de `PaginacionRequest`, que es una clase ([ver sección 3](#3-el-caso-de-listaradministradoresrequest)) |
| `RegistrarAdministradorRequestValidator` | `class` | Tiene comportamiento (reglas) y hereda de `AbstractValidator<T>` |
| `AdministradorService` | `class` | Tiene comportamiento y dependencias inyectadas (`db`, `cuentaService`) |
| `AdministradorProyecciones` | `static class` | Solo métodos y campos estáticos; no se instancia |

> **Regla práctica**
> - **DTOs** (datos que entran o salen) → `record`
> - **Cosas que hacen algo** (servicios, validadores) y **entidades** → `class`

---

## 2. Diferencias concretas

Las dos son **tipos por referencia**: `record` es en realidad un `record class`. Lo que hace un `record` es que **el compilador genera código extra por ti**.

### a) Sintaxis corta (record posicional)

```csharp
public record RegistrarAdministradorRequest(string Cedula, string Nombre, string Apellidos, string Telefono, string Correo);
```

Esa sola línea equivale aproximadamente a esto:

```csharp
public class RegistrarAdministradorRequest
{
    public RegistrarAdministradorRequest(string cedula, string nombre, string apellidos, string telefono, string correo)
    { Cedula = cedula; Nombre = nombre; /* ... */ }

    public string Cedula { get; init; }
    public string Nombre { get; init; }
    // ...
    // + Equals, GetHashCode, ToString, Deconstruct, operador ==, clonado para "with"...
}
```

### b) Inmutabilidad

Las propiedades de un record posicional son `init`: se asignan al construir el objeto y después **no se pueden cambiar**.

```csharp
var req = new RegistrarAdministradorRequest("1-1111-1111", "Ana", "Mora", "8888-8888", "ana@x.com");
req.Nombre = "Otra";   // ❌ error de compilación
```

Para un DTO es justo lo que quieres: el request que llegó es lo que llegó, y nadie debería alterarlo a mitad del flujo.

### c) Igualdad por valor *(la diferencia más importante)*

```csharp
var a = new AdministradorResponse(1, 5, "1-1111-1111", "Ana", ...);
var b = new AdministradorResponse(1, 5, "1-1111-1111", "Ana", ...);

a == b   // record → true  (compara el contenido)
         // class  → false (compara si es el MISMO objeto en memoria)
```

- En una **`class`**, dos objetos son iguales solo si son **la misma instancia**.
- En un **`record`**, son iguales si **todos sus valores coinciden**.

### d) Expresión `with` (copiar cambiando algo)

Como un record no se puede modificar, para "cambiarlo" creas una copia:

```csharp
var actualizado = response with { Activo = false };   // copia nueva; "response" no cambia
```

### e) `ToString()` útil

```csharp
Console.WriteLine(req);
// class  → Matricula.Application.Administradores.RegistrarAdministradorRequest
// record → RegistrarAdministradorRequest { Cedula = 1-1111-1111, Nombre = Ana, ... }
```

Esto ayuda al depurar.

> ⚠️ **Ojo con los logs:** un record que tenga una contraseña la imprimiría completa en su `ToString()`, y tu `AGENTS.md` prohíbe registrar contraseñas en logs.

### f) Herencia

Un `record` solo puede heredar de otro `record`, y una `class` solo de otra `class`. **No se pueden mezclar.**

---

## 3. El caso de `ListarAdministradoresRequest`

Es el único request que es `class`, por dos razones:

### 1. Hereda de `PaginacionRequest`, que es una clase

```csharp
public class PaginacionRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);            // corrige valores inválidos
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, PageSizeMaximo);
    }

    public string? Search { get; set; }
}
```

Por la regla de herencia, `ListarAdministradoresRequest` **no podría ser un record**.

### 2. `PaginacionRequest` necesita ser clase por cómo funciona

- Tiene **valores por defecto** (`page = 1`, `pageSize = 20`).
- Tiene **lógica dentro de los `set`** (el `Math.Clamp`).
- Se llena con **`[FromQuery]`**, a partir de la query string (`?page=2&search=ana`), donde todos los parámetros son opcionales.

ASP.NET crea el objeto vacío y va asignando solo las propiedades que vienen en la URL. Un patrón *"objeto con defaults + setters"* encaja mejor con una clase normal que con un record posicional, que exige pasar todo en el constructor.

En cambio, los requests de **registrar** y **actualizar** llegan en el **body JSON**, completos, y se validan con FluentValidation. Ahí un record posicional es ideal.

---

## 4. ¿Y las entidades (`Administrador`, `Usuario`)?

Son `class`, y conviene que lo sean. **No uses records para entidades de EF Core**, por tres motivos:

1. **Identidad:** una entidad es "el administrador con `Id` 7" aunque cambien sus datos. La igualdad por valor del record contradice esa idea.
2. **Change Tracker:** EF sigue objetos por instancia y los modifica (`administrador.ActualizarDatos(...)`). Con un record inmutable eso choca.
3. **HashCode cambiante:** en un record, `GetHashCode` se calcula con los valores. Si los valores cambian, el objeto "se pierde" dentro de un `HashSet` o `Dictionary`, y EF usa ese tipo de colecciones internamente.

---

## Resumen

| | `class` | `record` |
|---|---|---|
| **Igualdad** | Por referencia (misma instancia) | Por valor (mismo contenido) |
| **Mutabilidad típica** | Mutable | Inmutable (`init`) |
| **`with`** | No | Sí |
| **`ToString()`** | Nombre del tipo | Muestra las propiedades |
| **Sintaxis corta posicional** | No\* | Sí |
| **Uso típico** | Servicios, validadores, entidades | DTOs, requests, responses, valores |

\* Desde C# 12 las clases tienen *primary constructors*, como `AdministradorService(IAppDbContext db, ...)`. Pero esos parámetros **no** se convierten en propiedades públicas ni generan `Equals`/`ToString`. Son solo parámetros disponibles dentro de la clase.