# Atributos de endpoint y `CancellationToken` en `AuthController`

## 1. Los atributos del endpoint

Esas líneas entre corchetes se llaman **atributos**. Sirven para agregar información (metadatos) a un método o a una clase, y ASP.NET y otras herramientas la leen.

Tu interpretación es correcta en lo general. Aquí va un poco más de detalle sobre cada uno:

```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting(LimitesPeticiones.Login)]
[ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
```

### `[HttpPost("login")]`

Define dos cosas a la vez:

- **El verbo HTTP:** solo responde a `POST`. Un `GET /login` daría **405**.
- **El tramo final de la ruta.** Se combina con la ruta de la clase base `ApiControllerBase`:

```csharp
[Route("api/v1/[controller]")]   // [controller] = nombre de la clase sin "Controller" → "auth"
```

Así, la URL completa queda **`POST /api/v1/auth/login`**.

### `[AllowAnonymous]`

Correcto: no exige token. Esto importa más de lo que parece, porque la API **exige sesión por defecto**. En `Seguridad/Politicas.cs` hay una política de respaldo que pide un usuario autenticado en todos los endpoints.

Sin `[AllowAnonymous]`, nadie podría hacer login: para entrar necesitaría un token, y el token lo da justamente el login. Por eso `cambiar-contrasena` no lleva ningún atributo de autorización y aun así está protegido.

### `[EnableRateLimiting(LimitesPeticiones.Login)]`

Correcto. Aplica la política `"login"` definida en `Seguridad/LimitesPeticiones.cs`:

```csharp
options.AddPolicy(Login, context => PorIp(context, limite: 10, ventana: TimeSpan.FromMinutes(1)));
```

- Permite **10 intentos por minuto por IP**.
- El intento 11 recibe un **429** con el header `Retry-After`, que le dice al cliente cuántos segundos esperar.
- Esto protege contra **fuerza bruta** de contraseñas.

### `[ProducesResponseType<...>(...)]`

Tu idea es correcta, con un matiz importante: **son solo documentación**. No cambian lo que hace el endpoint. Lo que hacen es:

- Le dicen al generador de **OpenAPI** (lo que ves en Scalar) qué códigos puede devolver el endpoint y con qué forma de JSON.
- El tipo genérico indica el cuerpo de la respuesta:
  - `TokenResponse` → el 200 devuelve el token.
  - `ValidationProblemDetails` → el 400 devuelve los errores de validación por campo.
  - `ProblemDetails` → los demás errores, con el formato estándar.

Como nada verifica que sean ciertos, pueden quedar desactualizados. Por eso conviene **mantenerlos sincronizados** con lo que realmente lanza el servicio. En login:

| Código | Quién lo produce |
|---|---|
| **200** | `Ok(...)` |
| **400** | FluentValidation (por ejemplo, correo con formato inválido) |
| **401** | `UnauthorizedException` en `AuthService` (credenciales incorrectas) |
| **403** | `ForbiddenException` con RN-29 (cuenta desactivada) |
| **429** | El rate limiter |

La conversión de excepción a código HTTP la hace `Common/GlobalExceptionHandler.cs`:

```csharp
ForbiddenException e => (StatusCodes.Status403Forbidden, ...)
```

---

## 2. El parámetro `CancellationToken`

Sirve para **avisar que el trabajo ya no hace falta** y que conviene detenerlo.

### De dónde sale

Tú nunca lo envías desde el cliente. ASP.NET ve un parámetro de tipo `CancellationToken` en la acción y le inyecta automáticamente `HttpContext.RequestAborted`.

Ese token se activa cuando **el cliente se desconecta**: cierra el navegador, cancela el `fetch`, se cae la red, etc.

### Qué hace a lo largo del flujo

Por eso tu `AGENTS.md` pide pasarlo **"de punta a punta"**:

```csharp
// Controller
Login(LoginRequest request, CancellationToken cancellationToken) =>
    Ok(await authService.LoginAsync(request, cancellationToken));

// Servicio
await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

Si el cliente se va a mitad del request:

1. El token se marca como cancelado.
2. EF Core y Npgsql lo detectan y le piden a PostgreSQL que cancele la consulta en curso.
3. Se lanza una `OperationCanceledException` y el request termina sin seguir gastando recursos.

### Por qué importa

Imagina un listado pesado que tarda 5 segundos y un usuario que, impaciente, refresca la página 10 veces:

- **Sin token:** el servidor ejecuta las 10 consultas completas, aunque nadie vaya a leer 9 de los resultados.
- **Con token:** las 9 abandonadas se cortan en cuanto el cliente se desconecta.

En una app pequeña casi no se nota, pero es una buena práctica que en producción ahorra CPU, conexiones a la BD y memoria.

### Detalles a tener en cuenta

> - **No es un timeout.** Por sí solo no corta un request lento. Solo reacciona a una cancelación, que aquí viene de la desconexión del cliente.
> - **Solo sirve si lo pasas.** Si un método lo recibe pero no se lo da a `SingleOrDefaultAsync`, `SaveChangesAsync`, etc., la cadena se rompe en ese punto y esa operación ya no se cancela.
> - **Es opcional en las firmas** (`= default`). Si no lo pasas, compila igual, así que es fácil olvidarlo, y por eso el proyecto lo marca como regla.