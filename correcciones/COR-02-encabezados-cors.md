# COR-02: Encabezados de respuesta que el frontend no puede leer por CORS

| Estado | Fecha | Origen | Módulos afectados |
|---|---|---|---|
| **Cerrado** | 2026-11-23 | Brecha detectada al implementar RUI-03 en el frontend (mensaje del `429`) | `fundacion`, `acceso`, `estudiantes` |

## Contexto

El frontend corre en otro origen (en desarrollo, `http://localhost:5173`). Desde otro origen, el navegador solo deja leer a JavaScript los encabezados de respuesta "seguros" de CORS: `Cache-Control`, `Content-Language`, `Content-Length`, `Content-Type`, `Expires`, `Last-Modified` y `Pragma`. Cualquier otro encabezado que el frontend necesite leer, la API lo tiene que declarar en `Access-Control-Expose-Headers`.

| Punto | Brecha | Decisión | Estado |
|---|---|---|---|
| C-01 | `Retry-After` del `429` no es legible desde el frontend | Resolver: exponerlo en CORS | Completado |
| C-02 | `Location` del `201` no es legible desde el frontend | Resolver: exponerlo en CORS | Completado |

---

## C-01: el frontend no puede leer `Retry-After`

**Problema.** Ante un `429`, la API envía `Retry-After` con los segundos de espera, pero la política CORS no lo expone. Desde `localhost:5173`, `response.headers.get("Retry-After")` devuelve `null`, y el cuerpo del `429` no trae los segundos. El frontend no puede mostrar "Intente de nuevo en N segundos" ni deshabilitar el envío ese tiempo exacto (RUI-03).

**Verificación en el código:**
- `LimitesPeticiones.ResponderAsync` (`src/Matricula.Api/Seguridad/LimitesPeticiones.cs`) escribe `Retry-After` con los segundos que quedan de la ventana. El `ProblemDetails` solo trae un `detail` genérico.
- La política CORS de `Program.cs` tenía `WithOrigins`, `AllowAnyHeader` y `AllowAnyMethod`, pero no `WithExposedHeaders`.
- `UseCors()` va antes de `UseRateLimiter()`, así que el `429` sí lleva `Access-Control-Allow-Origin` y el frontend puede leer el cuerpo. El encabezado es lo único que le falta.

**El plan B del frontend no alcanza.** Mientras no pudiera leer el encabezado, el frontend iba a deshabilitar el envío 60 segundos. Pero las ventanas de las políticas son distintas:

| Política | Límite | Espera máxima |
|---|---|---|
| `login` | 10 por minuto | 60 s |
| `olvido-contrasena` | 5 cada 15 minutos | 900 s |
| `registro-estudiante` | 10 cada 15 minutos | 900 s |

En el olvido de contraseña y el registro público, el frontend volvería a habilitar el botón antes de tiempo y el usuario recibiría otro `429`. Solo el encabezado da la espera correcta en los tres casos.

**Opciones evaluadas:**

| Opción | Ventaja | Desventaja |
|---|---|---|
| a. Exponer `Retry-After` en la política CORS | Una línea; usa el encabezado estándar que el frontend ya espera para RUI-03 | Ninguna relevante |
| b. Agregar los segundos al `ProblemDetails` (extensión `retryAfter`) | No depende de CORS | Duplica el dato del encabezado y cambia el contrato del `429` |

**Decisión:** opción **a**. Los segundos de espera no revelan nada que el cliente no pueda deducir por su cuenta, así que exponerlos no es un riesgo.

**Documentos afectados:** `specs/SPEC-fundacion.md` (alcance de CORS, verificación e historial), `README.md` (sección CORS), `docs/arquitectura.md` §4 y `specs/mapa-modulos.md` (decisión 14).

**Estado:** Completado.

---

## C-02: el frontend no puede leer `Location`

**Problema.** `docs/arquitectura.md` §3 fija `201 + Location` al crear, y los controllers lo cumplen con `CreatedAtAction`. Pero `Location` tampoco es un encabezado seguro de CORS: desde el frontend no se puede leer, por ejemplo para ir al detalle de la matrícula o del grupo recién creado. Se detectó al analizar C-01.

**Verificación en el código:** `CreatedAtAction` en `AdministradoresController`, `AulasController`, `CursosController`, `EstudiantesController` (registro por un administrador), `GruposController` (crear grupo y agregar horario), `MatriculasController` (matricular y cambio de grupo), `MeController` (matricularse), `PeriodosController` y `ProfesoresController`. El registro público de estudiantes no envía `Location` a propósito, porque el visitante no puede consultar el recurso creado; no cambia.

**Decisión:** resolver junto con C-01, en la misma línea. Es el mismo mecanismo y el mismo costo, y el frontend no tiene que deducir la ruta del recurso a partir del cuerpo. El cuerpo del `201` sigue trayendo el recurso con su `id`, así que el frontend puede usar cualquiera de los dos.

**Documentos afectados:** los mismos de C-01.

**Estado:** Completado.

---

## Implementación (2026-11-23)

- `Program.cs`: la política CORS por defecto agrega `.WithExposedHeaders(HeaderNames.RetryAfter, HeaderNames.Location)`, con las constantes de `Microsoft.Net.Http.Headers` (ya incluido en ASP.NET Core; sin paquetes nuevos).
- El encabezado `Access-Control-Expose-Headers` solo se envía en respuestas a un origen permitido. Con `Cors:OrigenesPermitidos` vacío no cambia nada.
- Documentación: `README.md` (sección CORS), `SPEC-fundacion.md` (alcance, verificación manual e historial), `docs/arquitectura.md` §4 y decisión 14 de `specs/mapa-modulos.md`.

## Resultado de la verificación

Con la API en marcha y la BD de desarrollo:

| Caso | Resultado |
|---|---|
| 11 `POST /auth/login` seguidos con `Origin: http://localhost:5173` | Los 10 primeros `401`; el 11.º `429` con `Retry-After: 60`, `Access-Control-Allow-Origin: http://localhost:5173` y `Access-Control-Expose-Headers: Retry-After,Location` ✅ |
| 6 `POST /auth/olvido-contrasena` seguidos (correo inexistente) | Los 5 primeros `200`; el 6.º `429` con `Retry-After: 900` y el mismo `Access-Control-Expose-Headers` ✅. Confirma que la espera de 60 s del plan B no alcanzaba |
| `429` con `Origin: http://otro.local` | Sin `Access-Control-Allow-Origin` ni `Access-Control-Expose-Headers` ✅ |
| Preflight `OPTIONS /auth/login` desde `localhost:5173` | `204` con `Access-Control-Allow-Origin`, sin cambios ✅ |
| `GET /cursos` sin token desde `localhost:5173` | `401` con `Access-Control-Expose-Headers: Retry-After,Location` ✅ |

No se probó un `201` real, para no crear registros en la BD de desarrollo. `Access-Control-Expose-Headers` lo agrega el middleware de CORS a toda respuesta de un origen permitido, sin importar el status (lo confirman el `429` y el `401`), y `Location` ya se enviaba antes de este cambio.

Pendiente en el frontend: comprobar desde el navegador que `response.headers.get("Retry-After")` y `response.headers.get("Location")` devuelven el valor.

`dotnet build` sin warnings y 1104 pruebas unitarias en verde.

---

## Historial

- 2026-11-23: documento creado. Se analizan C-01 y C-02; ambos se aprueban.
- 2026-11-23: C-01 y C-02 implementados y verificados manualmente.
- 2026-11-23: ambos puntos quedan en un solo commit. **Documento cerrado.**
