# Validación de entrada: `RespuestaModeloInvalido` vs `ValidationFilter`

las dos clases sirven para devolver un **400 Bad Request** cuando los datos que llegan no son válidos. Pero actúan en **momentos distintos** y atrapan **problemas distintos**:

- **`RespuestaModeloInvalido`**: el JSON llegó roto o vacío. No se pudo ni siquiera construir el objeto `LoginRequest`.
- **`ValidationFilter`**: el objeto se construyó bien, pero sus valores no cumplen las reglas. Aquí entra tu caso: falta la contraseña, el correo tiene un formato inválido, etc.

---

## 1. El recorrido de un request antes de llegar a tu código

Cuando llega `POST /api/v1/auth/login`, ASP.NET hace esto antes de ejecutar `Login(...)`:

```text
JSON del body
   │
   ▼
① Model binding: convierte el JSON en un LoginRequest
   │   ¿falló? (body vacío, JSON mal formado, tipo incorrecto)
   │      └─► RespuestaModeloInvalido  → 400  (y se detiene aquí)
   ▼
② ValidationFilter: ejecuta LoginRequestValidator (FluentValidation)
   │   ¿hay errores? (correo vacío, contraseña vacía, demasiado larga…)
   │      └─► 400 con la lista de errores  (y se detiene aquí)
   ▼
③ AuthController.Login(...) → AuthService.LoginAsync(...)
```

Gracias a esto, cuando tu código del controller y del servicio se ejecuta, **ya sabe que los datos son válidos**. No necesita revisar `if (string.IsNullOrEmpty(request.Contrasena))`.

---

## 2. `RespuestaModeloInvalido`: errores de formato

El **model binding** es el paso en que ASP.NET lee el JSON y lo convierte en tu record `LoginRequest(string Correo, string Contrasena)`. Si no puede hacerlo, registra el error en el `ModelState`. El atributo `[ApiController]` (que tienes en `ApiControllerBase`) revisa ese `ModelState` y, si hay errores, responde 400 automáticamente.

`RespuestaModeloInvalido` **reemplaza la respuesta que ASP.NET daría por defecto**. Se conecta en `Program.cs`:

```csharp
.ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear);
```

### Cuándo se dispara

| Lo que envías | Problema |
|---|---|
| Body vacío | No hay nada que convertir |
| `{ "correo": "a@b.com",` | JSON mal formado |
| `{ "correo": 123 }` | Un número donde se esperaba un string |
| `GET /administradores?page=abc` | `page` es `int` y `"abc"` no se puede convertir |

### Por qué existe

Por defecto, ASP.NET responde con mensajes en inglés y bastante técnicos, por ejemplo:

> *"The JSON value could not be converted to Matricula.Application.Auth.LoginRequest. Path: $.correo | LineNumber: 0 | BytePositionInLine: 13."*

Eso tiene dos problemas:

1. Está **en inglés**.
2. **Revela tipos internos** (`Matricula.Application.Auth.LoginRequest`).

Tu clase recorre los errores y los reemplaza por dos mensajes genéricos en español:

```csharp
var esElCuerpo = string.IsNullOrEmpty(clave) || clave == "$";   // "$" = la raíz del JSON
errores.AddModelError(clave, esElCuerpo
    ? "Falta el cuerpo de la solicitud o no es un JSON válido."
    : "El valor enviado no tiene el formato esperado.");
```

---

## 3. `ValidationFilter`: reglas de negocio de entrada

Este es el que responde a tu pregunta sobre el login.

### Qué es un filtro

Un **action filter** es código que MVC ejecuta justo antes (y/o después) de cada acción de un controller. Se registra para todos los controllers en `Program.cs`:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    ...
```

Es la misma idea que el middleware, pero **dentro de MVC** y con acceso a los argumentos ya construidos de la acción.

### Qué hace, paso a paso

**a) Recorre los argumentos de la acción.**

```csharp
foreach (var argumento in context.ActionArguments.Values.OfType<object>())
```

En `Login(LoginRequest request, CancellationToken ct)`, el argumento relevante es el `LoginRequest`.

**b) Busca un validador para cada argumento.**

```csharp
var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());  // IValidator<LoginRequest>
if (serviceProvider.GetService(tipoValidador) is not IValidator validador)
    continue;
```

Construye el tipo `IValidator<LoginRequest>` y le pregunta a la inyección de dependencias si hay un validador registrado para él. Si no lo hay (como pasa con el `CancellationToken`), lo salta.

> **¿Y cómo se registró `LoginRequestValidator`?** En `Application/DependencyInjection.cs`:
>
> ```csharp
> services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
> ```
>
> Esa línea busca todas las clases `AbstractValidator<T>` del proyecto y las registra. Por eso, cuando creas un validador nuevo, **se aplica solo**, sin tocar nada más.

**c) Ejecuta las reglas y junta los errores.**

```csharp
var resultado = await validador.ValidateAsync(new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
foreach (var error in resultado.Errors)
    errores.AddModelError(error.PropertyName, error.ErrorMessage);
```

Fíjate en que también pasa el token de cancelación (`RequestAborted`).

**d) Corta el request o lo deja seguir.**

```csharp
if (errores.ErrorCount > 0)
{
    var problem = problemDetailsFactory.CreateValidationProblemDetails(..., 400, "Los datos enviados no son válidos");
    context.Result = new BadRequestObjectResult(problem);
    return;          // ← NO llama a next(): la acción del controller nunca se ejecuta
}
await next();        // ← todo bien: continúa hacia AuthController.Login
```

- **Si hubo errores**, asigna `context.Result`, lo que **cortocircuita** el request: se responde 400 y el controller nunca se ejecuta.
- **Si no hubo errores**, `next()` deja seguir.

### Las reglas en sí: `LoginRequestValidator`

```csharp
RuleFor(r => r.Correo)
    .NotEmpty().WithMessage("Indique su correo.")
    .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.")
    .EmailAddress().WithMessage("El correo no tiene un formato válido.");

RuleFor(r => r.Contrasena)
    .NotEmpty().WithMessage("Indique su contraseña.")
    .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.");
```

---

## 4. Tu ejemplo concreto: login sin contraseña

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "correo": "ana@instituto.com" }
```

1. **Model binding:** el JSON es válido. Falta `contrasena`, pero eso no es un error de formato, así que `Contrasena` simplemente queda en `null`. `RespuestaModeloInvalido` no interviene.
2. **`ValidationFilter`:** ejecuta `LoginRequestValidator`, y `NotEmpty()` falla para `Contrasena`.
3. **Respuesta:**

```http
HTTP/1.1 400 Bad Request

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Los datos enviados no son válidos",
  "status": 400,
  "errors": {
    "Contrasena": [ "Indique su contraseña." ]
  },
  "traceId": "00-..."
}
```

El campo `errors` **agrupa los mensajes por campo**, así el frontend puede mostrar cada mensaje debajo de su input. Si faltaran los dos campos, vendrían ambos juntos: FluentValidation evalúa todas las reglas y no se detiene en el primer error.

Si en cambio envías `{ "correo": "ana@instituto.com",` (JSON cortado), se detiene en el **paso 1** y responde `RespuestaModeloInvalido` con *"Falta el cuerpo de la solicitud o no es un JSON válido."*

---

## 5. Un detalle de `Program.cs` relacionado

```csharp
// Los obligatorios los valida FluentValidation, con mensajes en español; sin esto, [ApiController]
// rechaza antes los string no anulables con mensajes en inglés.
options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
```

Como el proyecto tiene activados los **nullable reference types**, `string Contrasena` (sin `?`) significa "no debería ser `null`". Por defecto, ASP.NET trata eso como un `[Required]` implícito y rechaza el request en el **paso 1**, con un mensaje en inglés (*"The Contrasena field is required."*).

Esta opción desactiva ese comportamiento para que la decisión la tome **FluentValidation en el paso 2**, con tus mensajes en español.

---

## 6. ¿Y la contraseña incorrecta?

Esa **no** la manejan estas clases:

- **"Contraseña vacía"** es un problema de formato de la entrada y se resuelve sin consultar la BD. Es un **400** del `ValidationFilter`.
- **"Contraseña incorrecta"** requiere consultar la BD y comparar el hash, así que la detecta `AuthService`. Este lanza `UnauthorizedException`, y `GlobalExceptionHandler` la convierte en **401**.

| Situación | Quién responde | Código |
|---|---|---|
| JSON roto o body vacío | `RespuestaModeloInvalido` | **400** |
| Falta un campo o tiene un formato inválido | `ValidationFilter` + validador | **400** |
| Credenciales incorrectas | `AuthService` → `GlobalExceptionHandler` | **401** |
| Cuenta desactivada | `AuthService` → `GlobalExceptionHandler` | **403** |