# Cómo funciona el token (JWT) en la API

El proceso tiene dos fases:

1. **Emitir el token**: una vez, en el login.
2. **Verificar el token**: en cada request posterior.

---

## Fase 1: emitir el token (login)

### Paso 1: el cliente envía sus credenciales

```http
POST /api/v1/auth/login

{ "correo": "ana@instituto.com", "contrasena": "MiClave123" }
```

Antes de llegar al servicio pasan los filtros que ya vimos (rate limiting y validación).

### Paso 2: se busca al usuario por correo

En `AuthService.LoginAsync`:

```csharp
var correo = Usuario.NormalizarCorreo(request.Correo);
var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken)
    ?? throw new UnauthorizedException();
```

Si no existe, responde **401** con un mensaje genérico de credenciales incorrectas. **No** dice "ese correo no existe", para no revelar qué correos están registrados.

### Paso 3: se verifica la contraseña

```csharp
var definitiva = hasher.Verificar(usuario.PasswordHash, request.Contrasena);
```

La BD **no guarda la contraseña**, sino un **hash**: una "huella" de la que no se puede volver a la original. Para verificar, se calcula la huella de lo que escribió el usuario y se compara con la guardada.

Hay tres posibilidades:

- **Coincide con la contraseña definitiva** → se emite un token normal.
- **Coincide con una temporal vigente** → se emite un token que obliga a cambiar la contraseña (`debeCambiarContrasena: true`).
- **No coincide con ninguna** → **401**.

Además, si la cuenta está desactivada → **403**.

### Paso 4: se juntan los datos que irán en el token

```csharp
return EmitirToken(usuario, debeCambiarContrasena: false);
```

`EmitirToken` llama a `GeneradorToken.Generar(...)` con estos datos (en `Seguridad/GeneradorToken.cs`):

```csharp
var claims = new Dictionary<string, object>
{
    ["sub"]            = "7",                    // id del usuario
    ["email"]          = "ana@instituto.com",
    ["role"]           = "Administrador",
    ["security_stamp"] = "a3f9c2e1-...",         // lo explico más abajo
};
// + "must_change_password": "true"  solo si entró con una temporal
```

Cada uno de estos datos se llama **claim** ("afirmación"): son afirmaciones sobre ti.

### Paso 5: se le pone fecha de vencimiento y quién lo emitió

```csharp
var descriptor = new SecurityTokenDescriptor
{
    Issuer   = "matricula-api",    // quién lo emitió
    Audience = "matricula-app",    // para quién es
    IssuedAt = ahora,              // cuándo se emitió
    Expires  = ahora + 60 minutos, // cuándo vence (Jwt:MinutosVigencia)
    ...
};
```

### Paso 6: se firma (el "sello")

Este es **el paso clave**:

```csharp
SigningCredentials = new SigningCredentials(CrearClave(_options), SecurityAlgorithms.HmacSha256)
```

Se toma todo lo anterior y se le aplica una firma calculada con una **clave secreta** que solo conoce el servidor.

> 🔑 Por eso esa clave va en **user-secrets** y nunca en `appsettings.json`: quien la tenga puede fabricar gafetes falsos.

La firma garantiza que **nadie puede modificar el token**. Si alguien cambia `"role": "Estudiante"` por `"role": "Administrador"`, la firma ya no coincide y el servidor lo rechaza.

### Paso 7: se devuelve al cliente

```csharp
_handler.CreateToken(descriptor)   // produce el texto del token
```

```http
HTTP/1.1 200 OK

{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiraEn": "2026-10-22T15:30:00Z",
  "rol": "Administrador",
  "debeCambiarContrasena": false
}
```

### ¿Cómo es por dentro ese texto?

Es un **JWT** (*JSON Web Token*), compuesto por tres partes separadas por puntos:

```text
eyJhbGciOiJIUzI1NiJ9 . eyJzdWIiOiI3IiwiZW1haWwiOi... . SflK...
   └── cabecera ──┘     └──────── datos (claims) ──┘     └ firma ┘
```

> ⚠️ **Las dos primeras partes no están cifradas**, solo codificadas en Base64. Cualquiera puede pegar el token en jwt.io y leer los claims. La firma impide **modificarlos**, pero no impide **leerlos**. Por eso nunca se pone información sensible (como contraseñas) en un token.

---

## Fase 2: usar el token (cada request)

### Paso 1: el cliente lo envía en cada petición

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Paso 2: el middleware de autenticación lo revisa

`app.UseAuthentication()`, configurado en `Seguridad/SeguridadExtensions.cs`, hace estas comprobaciones **sin tocar la BD**:

```csharp
bearer.TokenValidationParameters = new TokenValidationParameters
{
    IssuerSigningKey = GeneradorToken.CrearClave(opciones), // ✔ ¿la firma es auténtica?
    ValidIssuer      = opciones.Issuer,                     // ✔ ¿lo emitió "matricula-api"?
    ValidAudience    = opciones.Audience,                   // ✔ ¿es para "matricula-app"?
    ClockSkew        = TimeSpan.FromMinutes(1)              // ✔ ¿no ha vencido? (con 1 min de tolerancia)
};
```

Si alguna comprobación falla → **401**.

### Paso 3: tu validación extra, el security stamp

Las comprobaciones anteriores tienen un hueco: ¿qué pasa si el usuario hace logout, cambia su contraseña o un administrador desactiva su cuenta? **El token seguiría funcionando hasta su vencimiento.**

Para resolverlo, el proyecto usa el **security stamp**. Es un valor aleatorio guardado en la tabla `usuario`, que también va dentro del token. En cada request, `Seguridad/ValidacionSecurityStamp.cs` lo compara con el de la BD:

```csharp
if (!await authService.SesionValidaAsync(usuarioId, stamp, ...))
    context.Fail("La sesión ya no es válida.");

// AuthService
db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Activo && u.SecurityStamp == securityStamp)
```

Cuando el usuario hace logout, cambia su contraseña o se desactiva su cuenta, **se genera un stamp nuevo en la BD**. Los tokens viejos llevan el stamp anterior, ya no coinciden y quedan rechazados al instante.

> **Siguiendo la analogía:** el gafete tiene un número de serie, y la organización lleva una lista de números de serie vigentes. Si cambias tu contraseña, tu número viejo sale de la lista.

### Paso 4: ASP.NET "sabe" quién eres

Si todo pasa, ASP.NET construye un `ClaimsPrincipal` (el "usuario actual") con los claims del token. A partir de ahí:

- **`ICurrentUser.UsuarioId`** lee el claim `sub`, y así `AuthService` sabe quién está haciendo el request.
- **`app.UseAuthorization()`** revisa el claim `role` y las políticas. Por ejemplo, si un endpoint es solo para administradores, o si `must_change_password` bloquea todo excepto el cambio obligatorio. Si no tienes permiso → **403**.

---

## Resumen visual

```text
═══════════ LOGIN (una vez) ═══════════
Cliente ── correo + contraseña ──► AuthService
                                     ├─ busca usuario en BD
                                     ├─ verifica hash de contraseña
                                     ├─ arma claims: id, correo, rol, stamp
                                     ├─ pone vencimiento (60 min)
                                     └─ firma con la clave secreta
Cliente ◄────────── token ──────────┘

═══════════ CADA REQUEST ═══════════
Cliente ── Authorization: Bearer <token> ──► UseAuthentication
                                               ├─ ¿firma válida?        (sin BD)
                                               ├─ ¿issuer/audience ok?  (sin BD)
                                               ├─ ¿no vencido?          (sin BD)
                                               └─ ¿stamp coincide y cuenta activa? (BD)
                                             UseAuthorization
                                               └─ ¿el rol/política permite este endpoint?
                                             Controller → Service
```

## Las 4 ideas clave

1. **El login se hace una sola vez.** Después, el token reemplaza a la contraseña.
2. **La firma impide falsificarlo**, pero el contenido es legible: nada secreto adentro.
3. **El vencimiento** limita cuánto sirve un token robado.
4. **El security stamp** permite invalidar tokens antes de que venzan (logout, cambio de contraseña, desactivación).