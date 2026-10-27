# Manejo de errores con `GlobalExceptionHandler`

**Sí.** Los controllers y servicios no capturan excepciones: las dejan subir, y `GlobalExceptionHandler` las atrapa todas en un solo lugar y las convierte en una respuesta HTTP con formato **`ProblemDetails`**.

---

## 1. La idea: lanzar y dejar subir

Cuando una regla falla, el servicio simplemente lanza una excepción. Por ejemplo, en `AdministradorService`:

```csharp
throw new BusinessRuleException("RN-13", $"Ya existe un administrador con la cédula {cedula}.");
```

La ejecución se corta ahí. La excepción sube por la pila de llamadas (**servicio → controller → ASP.NET**) sin que nadie la capture, hasta llegar al manejador global. Este la traduce a algo así:

```http
HTTP/1.1 409 Conflict

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Regla de negocio incumplida",
  "status": 409,
  "detail": "Ya existe un administrador con la cédula 1-1111-1111.",
  "code": "RN-13"
}
```

Ganas tres cosas:

- **Servicios limpios:** no saben nada de HTTP ni de status codes. Solo dicen qué salió mal.
- **Respuestas consistentes:** todos los errores de la API tienen la misma forma.
- **Un solo lugar** para cambiar cómo se reportan los errores.

---

## 2. Cómo se conecta: `Program.cs`

Son tres líneas:

```csharp
builder.Services.AddProblemDetails();                           // ① servicio que escribe ProblemDetails en JSON
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // ② registra tu manejador
...
var app = builder.Build();
...
app.UseExceptionHandler();                                      // ③ middleware que captura las excepciones
```

Lo importante es **③**. ASP.NET procesa cada request a través de un **pipeline de middlewares**, como capas de una cebolla. `UseExceptionHandler()` es de los primeros en tu `Program.cs`, así que envuelve a todos los demás:

```text
Request →  UseExceptionHandler      ← (try { ... } catch → GlobalExceptionHandler)
             UseHttpsRedirection
               UseCors
                 UseRateLimiter
                   UseAuthentication
                     UseAuthorization
                       Controller → Service → EF Core
                                              💥 throw
```

En la práctica, `UseExceptionHandler` es **un gran `try/catch` alrededor de todo lo que viene después**. Cuando una excepción llega hasta él, llama a `GlobalExceptionHandler.TryHandleAsync`.

> **Por eso el orden importa:** si estuviera al final, no capturaría lo que ocurre en los middlewares anteriores.

---

## 3. La clase por dentro

`src/Matricula.Api/Common/GlobalExceptionHandler.cs` implementa `IExceptionHandler`, que tiene un único método:

```csharp
ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
```

Devuelve `true` si logró manejar la excepción. El método hace cuatro pasos.

### Paso 1: detectar duplicados de PostgreSQL

```csharp
var violacionUnicidad = exception is DbUpdateException
{
    InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
} ? postgres : null;
```

Esto es **pattern matching**. Pregunta si la excepción es un `DbUpdateException` de EF cuya excepción interna sea un error de Postgres con código *"violación de unicidad"* (SQLSTATE `23505`). Si lo es, la guarda en `violacionUnicidad`.

**¿Por qué hace falta?** Los servicios ya validan duplicados antes (`ValidarCedulaDisponibleAsync`), pero dos requests simultáneos podrían pasar la validación a la vez. En ese caso, el índice único de la BD es quien finalmente rechaza el segundo. Es una **red de seguridad**.

### Paso 2: decidir status, título y código

```csharp
var (status, title, code) = exception switch
{
    BusinessRuleException e      => (409, "Regla de negocio incumplida", e.Code),
    NotFoundException            => (404, "Recurso no encontrado", null),
    ConflictException            => (409, "Conflicto", null),
    ForbiddenException e         => (403, "Acceso denegado", e.Code),
    UnauthorizedException        => (401, "No autenticado", ...),
    DbUpdateConcurrencyException => (409, "Conflicto de concurrencia", "CONCURRENCIA"),
    _ when violacionUnicidad is not null => (409, "Dato duplicado", "RN-13"),
    _                            => (500, "Error interno", null)
};
```

Es un `switch` sobre el tipo de la excepción, y es **la tabla central del sistema**. Las excepciones propias del proyecto son:

| Excepción | Capa | Significa | HTTP |
|---|---|---|---|
| `BusinessRuleException` | Domain | Se incumplió una RN/RF | **409** |
| `NotFoundException` | Application | No existe el recurso | **404** |
| `ConflictException` | Application | Conflicto genérico | **409** |
| `ForbiddenException` | Application | Autenticado pero sin permiso | **403** |
| `UnauthorizedException` | Application | Credenciales inválidas | **401** |

Cualquier otra excepción que no aparezca ahí (un `NullReferenceException`, la BD caída, etc.) cae en `_` y se convierte en **500**.

### Paso 3: decidir el mensaje (`detail`) y qué registrar en logs

```csharp
if (violacionUnicidad is not null)
{
    // ...
}
else if (status == StatusCodes.Status500InternalServerError)
{
    detail = "Ocurrió un error inesperado.";
    logger.LogError(exception, "Error no controlado procesando {Path}", httpContext.Request.Path);
}
else
{
    detail = exception.Message;
}
```

Esta es una **decisión de seguridad**:

- **Excepciones "esperadas" (las tuyas):** el mensaje lo escribiste tú pensando en el usuario, así que se devuelve tal cual.
- **Un 500:** el mensaje real podría revelar detalles internos (SQL, rutas, stack trace). Al cliente le llega solo *"Ocurrió un error inesperado"*, y el detalle completo va al log, donde tú lo puedes ver.
- **Violación de unicidad:** pasa lo mismo. El nombre de la restricción (`ix_administrador_cedula`) va al log, no al cliente, para no exponer la estructura de la BD.

### Paso 4: armar y escribir el `ProblemDetails`

```csharp
httpContext.Response.StatusCode = status;

var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
if (code is not null)
    problem.Extensions["code"] = code;          // agrega "code" al JSON

if (exception is BusinessRuleException { Datos: var datos })
    foreach (var (clave, valor) in datos)
        problem.Extensions[clave] = valor;      // datos extra, ej. "usuarioId": 7 en RN-39

return await problemDetailsService.TryWriteAsync(...);
```

`Extensions` permite agregar campos propios al JSON estándar. `AdministradorService` lo aprovecha:

```csharp
throw new BusinessRuleException("RN-39", "...Reactive su cuenta...",
    new Dictionary<string, object?> { ["usuarioId"] = existente.UsuarioId });
```

Así el cliente recibe `"usuarioId": 7` y puede ofrecer directamente el botón **"Reactivar"**.

---

## 4. ¿Por qué no se pierde nada al cortar con una excepción?

```csharp
var cuenta = await cuentaService.CrearCuentaConTemporalAsync(...);
db.Administradores.Add(administrador);
await db.SaveChangesAsync(cancellationToken); // ...nunca se llega a guardar
```

Los cambios solo existen en el **Change Tracker** hasta que se llama a `SaveChangesAsync`:

- Si algo lanza **antes**, el request termina, el `DbContext` se descarta y no se guardó nada.
- Si falla **durante** `SaveChangesAsync`, la transacción se revierte.

Por eso no hacen falta `try/catch` para "deshacer" cambios.

---

## 5. Entonces, ¿nunca se usa `try/catch`?

Sí se usa, pero **solo cuando vas a hacer algo útil con el error ahí mismo**. En tu proyecto hay exactamente dos casos.

### `Application/Correos/EnvioCorreos.cs`: un fallo del correo no tumba el registro

```csharp
try
{
    await emailSender.EnviarAsync(...);
}
catch (Exception e) when (e is not OperationCanceledException)
{
    LogFalloEnvio(logger, e, tipo, destinatario);
    return false;   // → AdministradorRegistradoResponse.CorreoEnviado = false
}
```

Cuando se envía el correo, el administrador **ya está guardado**. Si smtp4dev está apagado, no quieres devolver un 500 que haga creer que el registro falló. Por eso:

- se captura el error,
- se registra en el log,
- y se responde `CorreoEnviado: false` para que el cliente pueda reenviar la temporal.

El filtro `when (e is not OperationCanceledException)` deja pasar las cancelaciones, que no son errores del correo.

### `Infrastructure/Seed/AdminSeedService.cs`

Captura solo el caso de *"tabla no existe"* (`UndefinedTable`), que ocurre al arrancar si aún no corriste las migraciones.

### Regla práctica

> - **No sabes qué hacer con el error** → no lo captures; déjalo subir al manejador global.
> - **Puedes recuperarte o el error es "aceptable"** → captúralo, y lo más específico posible: un tipo concreto o un filtro `when`.
> - **Nunca** captures solo para volver a lanzar, ni para ocultar el error sin registrarlo.

### ¿Y `finally`?

Tampoco suele hacer falta, porque los recursos (`DbContext`, conexiones) los libera la **inyección de dependencias** al final del request. Cuando tú mismo creas un recurso desechable, se usa `using`, que por dentro es un `try/finally`.

---

## 6. Errores que no pasan por `GlobalExceptionHandler`

No todas las respuestas de error vienen de excepciones. Algunas las generan otras piezas:

| Error | Lo genera |
|---|---|
| **400** de validación | `ValidationFilter` (FluentValidation) y `RespuestaModeloInvalido`, antes de entrar al controller |
| **401/403** por token ausente o sin permiso | El middleware de autenticación/autorización (`...AutorizacionHandler`) |
| **429** | El rate limiter (`LimitesPeticiones.ResponderAsync`) |
| **404** de ruta inexistente, **405**, etc. | `UseStatusCodePages()`, que les pone cuerpo `ProblemDetails` |

Todos usan el mismo `IProblemDetailsService`, así que el cliente recibe siempre el mismo formato de error, venga de donde venga.