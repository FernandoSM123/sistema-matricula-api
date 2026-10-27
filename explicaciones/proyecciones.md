# Proyecciones: de `Administrador` a `AdministradorResponse`

`AdministradorProyecciones` convierte una entidad `Administrador` en el DTO `AdministradorResponse`. Lo hace de **dos maneras**, según de dónde venga el administrador:

- Desde una **consulta a la BD** → se traduce a SQL.
- Desde una **entidad ya cargada en memoria** → se ejecuta en C#.

```csharp
public static class AdministradorProyecciones
{
    public static readonly Expression<Func<Administrador, AdministradorResponse>> AResponse = a => new AdministradorResponse(
        a.Id,
        a.UsuarioId,
        a.Cedula,
        a.Nombre,
        a.Apellidos,
        a.Telefono,
        a.Usuario.Correo,
        a.Usuario.Activo,
        a.FechaCreacion,
        a.Usuario.UltimoIngreso);

    private static readonly Func<Administrador, AdministradorResponse> AResponseCompilada = AResponse.Compile();

    public static AdministradorResponse ToResponse(this Administrador administrador) => AResponseCompilada(administrador);
}
```

---

## 1. `AResponse`: para consultas a la BD

`AResponse` es una **`Expression`**, no una función normal. Una `Expression` es la **descripción** del código en forma de datos, y eso permite que EF Core la lea y la convierta en SQL.

Se usa con `.Select(...)` en `ListarAsync` y `ObtenerAsync`:

```csharp
db.Administradores.AsNoTracking()
    .Where(a => a.Id == id)
    .Select(AdministradorProyecciones.AResponse)
    .SingleOrDefaultAsync(cancellationToken);
```

EF Core genera algo así:

```sql
SELECT a.id, a.usuario_id, a.cedula, a.nombre, a.apellidos, a.telefono,
       u.correo, u.activo, a.fecha_creacion, u.ultimo_ingreso
FROM administradores a
JOIN usuarios u ON u.id = a.usuario_id
WHERE a.id = 5
```

Ventajas:

- **Solo trae las columnas necesarias.** El `PasswordHash` del usuario, por ejemplo, nunca sale de la BD.
- **No hace falta `Include(a => a.Usuario)`.** EF Core ve `a.Usuario.Correo` dentro de la expresión y hace el `JOIN` por su cuenta.
- **No se cargan entidades en memoria.** El DTO se construye directamente con el resultado del SQL.

---

## 2. `ToResponse()`: para una entidad ya cargada

A veces el objeto ya está en memoria, porque se acaba de crear o modificar. En `ActualizarAsync`, por ejemplo:

```csharp
var administrador = await db.Administradores
    .Include(a => a.Usuario)                  // carga también la cuenta
    .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ...;

administrador.ActualizarDatos(...);
await db.SaveChangesAsync(cancellationToken);

return administrador.ToResponse();            // no vuelve a la BD
```

No tiene sentido hacer otra consulta. Para eso está `ToResponse()`:

- **`.Compile()`** convierte la `Expression` en una función de C# normal (`Func`) que se ejecuta sobre el objeto en memoria.
- Se compila **una sola vez**, porque el campo es `static readonly`. Compilar una expresión es costoso, así que no conviene hacerlo en cada llamada.
- Es un **método de extensión** (`this Administrador`), por eso se escribe `administrador.ToResponse()`.

> **Ojo:** aquí **sí** hace falta el `Include(a => a.Usuario)`. Sin él, `a.Usuario` sería `null` y `a.Usuario.Correo` lanzaría `NullReferenceException`. Por eso el comentario dice *"para una entidad ya cargada junto con su cuenta"*.

---

## 3. Una sola fuente de verdad

¿Por qué no escribir dos mapeos por separado, uno para SQL y otro para memoria? Porque si mañana se añade un campo a `AdministradorResponse`, solo se cambia `AResponse`, y las dos rutas se actualizan juntas. Con dos mapeos escritos a mano es fácil actualizar uno y olvidar el otro.

| Situación | Se usa | Dónde se ejecuta |
|---|---|---|
| Consultar desde la BD | `.Select(AdministradorProyecciones.AResponse)` | En PostgreSQL (como SQL) |
| Ya tienes la entidad cargada | `administrador.ToResponse()` | En C# (en memoria) |

Además, así se cumple la regla del proyecto: **las entidades nunca salen de `Application`**, solo los DTOs.

---

## 4. Relación con AutoMapper

Esto es justo lo que hacen librerías como **AutoMapper**, y hasta tienen los mismos dos modos:

| Esta clase | AutoMapper | Para qué |
|---|---|---|
| `.Select(AdministradorProyecciones.AResponse)` | `.ProjectTo<AdministradorResponse>(config)` | Consulta a la BD, traducida a SQL |
| `administrador.ToResponse()` | `mapper.Map<AdministradorResponse>(administrador)` | Entidad ya en memoria |

Por dentro AutoMapper hace algo parecido. Empareja propiedades por nombre (`Nombre` → `Nombre`), aplana las anidadas por convención (`Usuario.Correo` → `UsuarioCorreo`) y con eso construye una `Expression` como la de esta clase y la compila.

### Por qué en este proyecto se hace a mano

`AGENTS.md` prohíbe AutoMapper. Las razones:

1. **Los errores aparecen al compilar, no en producción.** Si se renombra `Telefono` en la entidad, la proyección deja de compilar. Con AutoMapper compila igual y el campo llega `null` o falla en tiempo de ejecución.
2. **Se ve lo que pasa.** Con F12 sobre `AResponse` se ve cada campo y de dónde sale. Con AutoMapper el mapeo queda implícito en convenciones y perfiles.
3. **Los nombres no siempre coinciden.** El DTO tiene `Correo` y `Activo`, pero vienen de `a.Usuario.Correo` y `a.Usuario.Activo`. En AutoMapper habría que configurarlo con `ForMember(...)`: casi el mismo código, pero menos directo.
4. **Sin fugas accidentales.** Un mapeo automático puede copiar una propiedad que no se quería exponer si alguien la añade al DTO con el mismo nombre. A mano, solo sale lo que se escribe.
5. **Es poco código.** Unas diez líneas por DTO. Una librería con su configuración no compensa.

### Un punto intermedio: Mapperly

**Mapperly** genera el código de mapeo **al compilar** (source generators). No usa reflexión en tiempo de ejecución y los errores salen en el build: es como escribirlo a mano, pero automático. Aun así, añadir un paquete NuGet requiere aprobación según `AGENTS.md`, y con el volumen actual de DTOs el mapeo manual basta.

> **Regla práctica**
> - Una **`Expression`** por DTO: es la única definición del mapeo.
> - Consultas → **`.Select(expresión)`**. Entidad en memoria → **`.ToResponse()`**.
> - Nada de AutoMapper.

---
