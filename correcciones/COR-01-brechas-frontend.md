# COR-01: Brechas detectadas al planear el frontend

| Estado | Fecha | Origen | Módulos afectados |
|---|---|---|---|
| **Cerrado** | 2026-11-17 | Sección 8, "Brechas con el backend", del plan del frontend | `catalogos`, `acceso` |

## Contexto

Al planear el frontend se registraron cuatro brechas (B-01 a B-04): cosas que el frontend necesita y que la API no ofrece, o no ofrece de forma cómoda. Se revisó cada una contra el código para decidir si se resuelve en el backend o queda fuera del alcance.

| Punto | Brecha | Decisión | Estado |
|---|---|---|---|
| B-01 | La oferta no indica el periodo y `GET /periodos/vigente` exige sesión | Resolver: `/periodos/vigente` sin sesión | Completado |
| B-02 | `GET /me` no trae el id de la persona | Resolver: `PersonaId` en el perfil | Completado |
| B-03 | No se exponen las entidades auditadas | Fuera del alcance | Descartado |
| B-04 | El mensaje del olvido tutea; el resto trata de usted | Resolver: "recibirá" | Completado |

---

## B-01: la oferta académica no indica el periodo

**Problema.** `GET /api/v1/oferta-academica` es público, pero no incluye el periodo. `GET /api/v1/periodos/vigente`, que lo daría, exige sesión. El visitante ve la oferta sin saber de qué periodo es ni hasta cuándo puede matricular.

**Verificación en el código:**
- `OfertaAcademicaResponse` (`src/Matricula.Application/Grupos/Consultas/OfertaAcademicaResponse.cs`) no tiene datos del periodo.
- `PeriodosController.ObtenerVigente` (`src/Matricula.Api/Controllers/PeriodosController.cs`) tiene `[Authorize]`.

**Opciones evaluadas:**

| Opción | Ventaja | Desventaja |
|---|---|---|
| a. Agregar el periodo a la respuesta de la oferta | Una sola petición | Cambia el contrato: habría que envolver el `PagedResult` o repetir el periodo en cada grupo |
| b. Permitir `GET /periodos/vigente` sin sesión | Un solo atributo; no rompe ningún contrato | Una petición más desde el frontend |

**Decisión:** opción **b**. El nombre y las fechas de un periodo no son datos sensibles, y el cambio no altera ninguna respuesta existente.

**Cambio propuesto:**
- `[AllowAnonymous]` en `PeriodosController.ObtenerVigente`. El resto de los endpoints de periodos siguen exigiendo sesión.
- Actualizar el comentario XML del endpoint y la entrada de `Matricula.Api.http`.

**Comportamiento a tener en cuenta en el frontend:** sin periodo vigente, la oferta responde `200` con la lista vacía (supuesto 8 de `grupos`), mientras que `/periodos/vigente` responde `404`. El frontend debe interpretar ese `404` como "no hay oferta", no como un error.

**Documentos afectados:**
- `docs/requerimientos.md`:
  - §7 Seguridad: agregar "la consulta del periodo vigente" a las excepciones que no requieren sesión.
  - §2 (rol Visitante) y la matriz §2.2: el visitante también consulta el periodo vigente.
- `specs/SPEC-catalogos.md`: historial y alcance de RF-38.

**Verificación manual:**
- `GET /periodos/vigente` sin token → `200` con el periodo, o `404` si no hay ninguno vigente.
- `GET /periodos` y `GET /periodos/{id}` sin token siguen respondiendo `401`.

**Implementación (2026-11-17):**
- `PeriodosController.ObtenerVigente` pasa de `[Authorize]` a `[AllowAnonymous]`, como la oferta académica. Se quitan sus respuestas `401` y `403` de OpenAPI y se actualiza su comentario XML.
- Documentación:
  - `docs/requerimientos.md`: rol Visitante (§2), una fila nueva en la matriz §2.2, RF-38 y las excepciones de §7.
  - `SPEC-catalogos.md`: usuarios, tabla de endpoints e historial.
  - `README.md`: sección de endpoints sin sesión, que tampoco mencionaba la oferta académica.
  - `Matricula.Api.http`: la petición ya no envía token.
  - `specs/mapa-modulos.md`: decisión 14.
- Sin límite de peticiones, igual que la oferta: es una consulta de solo lectura de un único registro.

**Resultado de la verificación** (BD de desarrollo con los datos de prueba):

| Caso | Resultado |
|---|---|
| `GET /periodos/vigente` sin token | `200` con el periodo `2026-T4` (matrícula del 2026-11-06 al 2026-11-26) ✅ |
| `GET /periodos/vigente` con un token inválido | `200`: el endpoint ignora el token ✅ |
| `GET /periodos/vigente` con token de estudiante | `200` ✅ |
| `GET /periodos` y `GET /periodos/1` sin token | `401` ✅ |
| `GET /oferta-academica` sin token | `200` (sin cambios) ✅ |

No se probó el `404` sin periodo vigente, porque habría que modificar los periodos de la BD de desarrollo. Esa lógica no cambió y se verificó al cerrar `catalogos`.

`dotnet build` sin warnings y 1104 pruebas unitarias en verde.

**Estado:** Completado.

---

## B-02: `GET /me` no trae el id de la persona

**Problema.** `GET /api/v1/me` devuelve el `usuarioId`, pero no el id del registro de administrador que pide `PUT /administradores/{id}`. Para "Editar mis datos", el frontend tiene que buscarse con `GET /administradores?search=<su correo>` y quedarse con el que tenga su `usuarioId`: una petición extra y un rodeo que depende de la búsqueda.

**Verificación en el código:**
- `PerfilResponse` (`src/Matricula.Application/Auth/PerfilResponse.cs`) trae `UsuarioId` y `DatosPersonales`, pero ningún id de la persona.
- `AuthService` ya consulta la tabla de la persona según el rol para armar `DatosPersonales`, así que el id está a mano.

**Decisión:** resolver. Es un cambio aditivo y de costo mínimo, y elimina un rodeo frágil.

**Cambio propuesto:**
- Agregar `int? PersonaId` a `PerfilResponse`, con el id del administrador, estudiante o profesor según el rol. Es `null` solo si la cuenta no tiene persona asociada, lo que RN-31 no permite.
- Se aplica a los tres roles para que el contrato sea uniforme, aunque hoy solo el administrador lo necesita. El estudiante edita sus datos con `/me/datos-contacto` y el profesor no edita los suyos.

**Documentos afectados:** `specs/SPEC-acceso.md` (historial y respuesta del perfil).

**Verificación manual:** `GET /me` con un token de cada rol devuelve el `personaId` correcto, y con el del administrador funciona `PUT /administradores/{personaId}`.

**Implementación (2026-11-17):**
- `PerfilResponse` tiene `int? PersonaId` justo después de `UsuarioId`.
- `AuthService.ObtenerPerfilAsync` proyecta el `Id` junto con los datos personales de cada tabla, en la misma consulta. No agrega consultas a la BD.
- Documentación: comentario de `GET /me` en `Matricula.Api.http` e historial y alcance de `SPEC-acceso.md`.

**Resultado de la verificación** (BD de desarrollo con los datos de prueba):

| Cuenta | Resultado |
|---|---|
| `admin@instituto.local` | `personaId: 1`; `GET /administradores/1` devuelve el mismo `usuarioId` (1) ✅ |
| `profesor1@prueba.test` | `personaId: 1`, rol Profesor ✅ |
| `estudiante1@prueba.test` | `personaId: 1`, rol Estudiante ✅ |
| Profesor con id 3 | `personaId: 3` ✅ |
| Estudiante con id 3 | `personaId: 3` ✅ |

No se probó el `PUT /administradores/{personaId}` para no modificar los datos de la BD de desarrollo. El `GET` con el mismo id confirma que el id es el correcto.

`dotnet build` sin warnings y 1104 pruebas unitarias en verde.

**Estado:** Completado.

---

## B-03: la API no expone las entidades auditadas

**Problema.** El filtro `entidad` de `GET /api/v1/auditoria` solo acepta ciertos valores, que la API nombra únicamente en el mensaje del `400`. El frontend mantiene una lista fija.

**Verificación en el código:** `ReglasAuditoria.EntidadesAuditadas` (`src/Matricula.Domain/Reglas/ReglasAuditoria.cs`) se arma por reflexión con las clases que heredan de `EntidadBase`, para que una entidad nueva quede auditada sin tocar ninguna lista (supuesto 7 de `auditoria`).

**Decisión: descartado.** Se mantiene la lista fija en el frontend. Razones:
- Publicar los valores como enum en OpenAPI obligaría a duplicar la lista en un enum de C#, lo que va contra el diseño por reflexión.
- Agregar una entidad requiere una spec nueva: es un cambio raro y planificado, y en ese momento se actualiza el frontend.
- El frontend necesita su propio mapa de etiquetas igualmente (la API usa `Matricula` y la interfaz muestra "Matrícula"), así que una lista dinámica mostraría las entidades nuevas con su nombre técnico.

Si en el futuro hiciera falta, la alternativa más simple es un endpoint de solo lectura `GET /auditoria/entidades` que devuelva `EntidadesAuditadas`.

**Estado:** Descartado.

---

## B-04: trato inconsistente en el mensaje del olvido de contraseña

**Problema.** Los mensajes de la API tratan al usuario de usted ("Contacte a la administración", "Reactívelo", "Intente de nuevo"), salvo el del olvido de contraseña, que lo tutea: "Si el correo está registrado, recibirás una contraseña temporal." El frontend lo muestra tal cual, así que en esa vista el trato no coincide con el resto de la interfaz.

**Verificación en el código:** `AuthService.MensajeOlvido` (`src/Matricula.Application/Auth/AuthService.cs`) es el único mensaje en segunda persona informal. Las plantillas de correo también usan usted.

**Decisión:** resolver. Es solo de estilo, pero el costo es mínimo.

**Cambio propuesto:** `MensajeOlvido` pasa a "Si el correo está registrado, recibirá una contraseña temporal." RN-28 solo exige que la respuesta sea siempre la misma, así que la regla no cambia.

**Documentos afectados:**
- `docs/requerimientos.md`: el texto citado en RF-10.
- `specs/SPEC-acceso.md` y `src/Matricula.Api/Matricula.Api.http`, si citan el mensaje.

**Verificación manual:** `POST /auth/olvido-contrasena` con un correo existente y con uno inexistente devuelve el mismo mensaje, con "recibirá".

**Implementación (2026-11-17):**
- `AuthService.MensajeOlvido` cambia a "recibirá".
- Documentación: texto de RF-10 en `docs/requerimientos.md`, comentario del olvido en `Matricula.Api.http` e historial de `SPEC-acceso.md`.

**Resultado de la verificación:**
- `POST /auth/olvido-contrasena` con `no-existe@prueba.test` → `200` con `"Si el correo está registrado, recibirá una contraseña temporal."` ✅
- No se probó con un correo existente, para no generar una temporal en la BD de desarrollo. Ambos casos devuelven la misma constante en el mismo `return` (`AuthService`), así que el mensaje es idéntico.

**Estado:** Completado.

---

## Historial

- 2026-11-17: documento creado. Se analizaron B-01 a B-04 contra el código. B-01, B-02 y B-04 se aprueban para implementar; B-03 se descarta.
- 2026-11-17: B-04 y B-02 implementados y verificados manualmente.
- 2026-11-17: B-01 implementado y verificado. Los tres puntos quedan en un solo commit. **Documento cerrado.**
