# COR-03: El OpenAPI declara mal los números y los enums

| Estado | Fecha | Origen | Módulos afectados |
|---|---|---|---|
| **Cerrado** | 2026-11-24 | Brechas B-06 y B-07, detectadas en `nucleo` T1 del frontend (generación de tipos con `pnpm api:tipos`) | `fundacion` |

## Contexto

El frontend genera sus tipos TypeScript a partir de `openapi/v1.json` (`docs/arquitectura.md` §3.1). Si el documento no coincide con lo que la API envía y acepta, los tipos salen más anchos o directamente incorrectos.

ASP.NET Core tiene dos juegos de opciones JSON:
- **MVC** (`AddControllers().AddJsonOptions`): las que usan los controllers para leer los bodies y escribir las respuestas.
- **HTTP** (`ConfigureHttpJsonOptions`): las que usan el generador del OpenAPI (`Microsoft.AspNetCore.OpenApi`), aunque los endpoints sean controllers, y los `ProblemDetails` que escribe `IProblemDetailsService`.

`Program.cs` solo configuraba las de MVC, así que el OpenAPI se generaba con los valores web por defecto.

| Punto | Brecha | Decisión | Estado |
|---|---|---|---|
| C-01 (B-06) | Los números se declaran `integer \| string` | Resolver: `NumberHandling = Strict` en los dos juegos de opciones | Completado |
| C-02 (B-07) | Los 9 enums se declaran `integer`, sin sus valores | Resolver: `JsonStringEnumConverter` en los dos juegos de opciones | Completado |
| C-03 | `Rol` y `MotivoRetiro` incluyen `null` entre sus valores | Resolver: transformador de esquema que quita el `null` de los enums | Completado |

---

## C-01: los números se declaran `integer | string`

**Problema.** El OpenAPI declaraba 168 números como `"type": ["integer", "string"]` con un `pattern` de entero (por ejemplo, `GrupoResponse.id`, `usuarioId`, `personaId`, `page`, `totalCount` o `status`). La API los envía como números. `pnpm api:tipos` generaba `number | string`, y cada uso de un id o un número tenía que estrecharlo o convertirlo.

**Verificación en el código:** las opciones HTTP no estaban configuradas. Los valores web por defecto (`JsonSerializerDefaults.Web`) incluyen `NumberHandling = AllowReadingFromString`, y el generador lo traduce en `integer | string`. Las opciones de MVC tienen el mismo valor por defecto, así que los controllers también aceptaban `"cupo": "30"` en los bodies.

**Opciones evaluadas:**

| Opción | Ventaja | Desventaja |
|---|---|---|
| a. `Strict` solo en las opciones HTTP | Corrige el OpenAPI | El contrato dice `integer`, pero la API sigue aceptando texto |
| b. `Strict` en los dos juegos de opciones | El contrato y lo que la API acepta coinciden | Un número enviado como texto pasa a responder `400` |
| c. Tipo utilitario en `modelos.ts` del frontend | No toca la API | Tapa el problema en cada cliente; el contrato sigue mal |

**Decisión:** opción **b**. El `400` sale con el mensaje en español de `RespuestaModeloInvalido` ("El valor enviado no tiene el formato esperado."), sin nombres de tipos internos. `Matricula.Api.http` no envía números como texto: los valores entre comillas (`cedula`, `telefono`, `nrc`) son strings en los DTO.

**Documentos afectados:** `specs/SPEC-fundacion.md` (alcance, verificación e historial), `docs/arquitectura.md` §3 y `specs/mapa-modulos.md` (decisión 14).

**Estado:** Completado.

---

## C-02: los enums se declaran `integer`

**Problema.** El OpenAPI declaraba los 9 enums (`Rol`, `Modalidad`, `EstadoGrupo`, `EstadoMatricula`, `MotivoRetiro`, `NivelCurso`, `DiaSemana`, `AccionAuditoria` y `OrigenError`) como `"type": "integer"`, sin sus valores. La API los envía como texto (`"modalidad": "Presencial"`). Los tipos generados no eran solo más anchos: eran incorrectos (`Rol: number`).

**Verificación en el código:** `JsonStringEnumConverter` estaba registrado solo en `AddControllers().AddJsonOptions` (`Program.cs`), y el generador del OpenAPI lee las opciones HTTP.

**Decisión:** registrar el convertidor también en las opciones HTTP. Se descartó escribir las uniones de textos a mano en `modelos.ts`, porque duplicaba los valores de cada enum y se desfasaría al cambiarlos. Así los `ProblemDetails` de `IProblemDetailsService` también escribirían los enums como texto si alguna vez llevaran uno (hoy no llevan).

`JsonStringEnumConverter` mantiene `allowIntegerValues` en `true`, como antes: la API sigue aceptando un enum enviado como número, y FluentValidation rechaza los valores fuera del enum con `IsInEnum`. Rechazarlos en la lectura sería un endurecimiento aparte, fuera de esta corrección.

**Documentos afectados:** los mismos de C-01.

**Estado:** Completado.

---

## C-03: `Rol` y `MotivoRetiro` incluyen `null` entre sus valores

**Problema.** Se detectó al verificar C-02. Los esquemas de componente quedan así:

```json
"Rol":          { "enum": ["Administrador", "Profesor", "Estudiante", null] }
"MotivoRetiro": { "enum": ["RetiroVoluntario", "CambioGrupo", "CancelacionGrupo", null] }
```

Los dos enums se usan como anulables en algunos DTO (`Rol? UsuarioRol` en `RegistroAuditoriaResponse`, `RegistroErrorResponse` y `RegistroErrorDetalleResponse`; `MotivoRetiro? MotivoRetiro` en `MatriculaResponse` y `EstudianteGrupoResponse`). En esas propiedades el generador ya expresa la nulabilidad con `oneOf: [{ "type": "null" }, { "$ref": ... }]`, pero además deja el `null` dentro del esquema compartido. Con eso, `PerfilResponse.rol` y `TokenResponse.rol`, que nunca son nulos, se generarían como `Rol | null` en el frontend.

**Impacto:** menor. El frontend tendría que estrechar `rol` en el login y el perfil. `MotivoRetiro` solo se usa como anulable, así que ahí no cambia nada en la práctica.

**Opciones:**

| Opción | Ventaja | Desventaja |
|---|---|---|
| a. Un `IOpenApiSchemaTransformer` que quite `null` del `enum` de los esquemas de enums (la nulabilidad ya la da el `oneOf` de cada propiedad) | `Rol` queda exacto en todos los usos; unas pocas líneas, sin paquetes | Código propio para compensar un detalle del generador |
| b. No hacer nada | Sin código | `rol` se genera como `Rol \| null` en el login y el perfil |

**Decisión:** opción **a**, aprobada por el usuario. No cambia el comportamiento de la API, solo el documento OpenAPI.

**Documentos afectados:** `specs/SPEC-fundacion.md` (alcance e historial).

**Estado:** Completado.

---

## Implementación (2026-11-24)

- `src/Matricula.Api/Common/OpcionesJson.cs` (nuevo): `Configurar(JsonSerializerOptions)` agrega `JsonStringEnumConverter` y fija `NumberHandling = JsonNumberHandling.Strict`. Así los dos juegos de opciones no vuelven a separarse.
- `Program.cs`: `AddJsonOptions` y el nuevo `ConfigureHttpJsonOptions` llaman a `OpcionesJson.Configurar`.
- `src/Matricula.Api/Common/EnumSinNullSchemaTransformer.cs` (nuevo, C-03): un `IOpenApiSchemaTransformer` que, en los esquemas de un enum (o de un enum anulable), quita los `null` de la lista `enum`. Se registra en `AddOpenApi` junto a `BearerSecuritySchemeTransformer`.
- Sin paquetes nuevos ni cambios de esquema de BD.
- Documentación: `SPEC-fundacion.md` (alcance, verificación manual e historial), `docs/arquitectura.md` §3 y la decisión 14 de `specs/mapa-modulos.md`.

## Resultado de la verificación

`openapi/v1.json` antes del cambio (instancia en `:5022`) y después (instancia aparte en `:5023`, con la BD de desarrollo):

| Caso | Antes | Después |
|---|---|---|
| Esquemas con tipo `integer \| string` o `number \| string` | 168 | 0 ✅ |
| Esquemas con `pattern` numérico | 214 | 0 ✅ |
| `GrupoResponse.id` | `["integer", "string"]` + `pattern` | `"integer"`, `int32` ✅ |
| Los 9 enums | `"type": "integer"`, sin valores | `enum` con sus valores de texto, sin `null` ✅ |
| `PerfilResponse.rol` y `TokenResponse.rol` (C-03) | — | `$ref` a `Rol`, que ya no admite `null` ✅ |
| `RegistroAuditoriaResponse.usuarioRol` y `MatriculaResponse.motivoRetiro` (C-03) | — | Siguen anulables: `oneOf: [{ "type": "null" }, { "$ref": ... }]` ✅ |
| `GET /api/v1/oferta-academica` y `GET /api/v1/periodos/vigente` | — | Mismo formato: ids numéricos, `"modalidad": "Presencial"`, `"dia": "Lunes"` ✅ |
| `401` sin token | — | `ProblemDetails` con `"status": 401` numérico ✅ |

La lectura estricta se comprobó con `dotnet fsi`, con las mismas opciones (`JsonSerializerDefaults.Web` + `JsonStringEnumConverter` + `Strict`): `{"cupo":"30"}` lanza `JsonException` (antes se aceptaba), y `{"cupo":30}` y los enums como texto se leen igual que antes. MVC convierte esa excepción en el `400` de `RespuestaModeloInvalido`.

**Verificación a cargo del usuario** (requiere un token de administrador): en Scalar, crear un grupo con `"cupo": "30"` y comprobar el `400` con el mensaje en español. Si se reinicia la API en `:5022`, `pnpm api:tipos` en el frontend debe generar `number` y las uniones de textos.

`dotnet build` sin warnings y 1104 pruebas unitarias en verde.

---

## Historial

- 2026-11-24: documento creado. C-01 y C-02 aprobados (B-06 y B-07 del frontend) e implementados. Al verificar se detecta C-03, pendiente de decisión.
- 2026-11-24: C-03 aprobado (opción a), implementado y verificado en el OpenAPI.
- 2026-11-24: el usuario aprueba el cierre; los tres puntos quedan en un solo commit. **Documento cerrado.**
