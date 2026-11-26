# COR-04: Nombres de las pruebas unitarias en inglés, con descripción en español

| Estado | Fecha | Origen | Módulos afectados |
|---|---|---|---|
| **Cerrado** | 2026-11-26 | Pedido del usuario al revisar `RefreshTokenTests` (módulo `sesiones`) | Todos los que tienen pruebas: `fundacion`, `acceso`, `estudiantes`, `profesores`, `catalogos`, `grupos`, `matriculas`, `auditoria`, `errores`, `datos-prueba`, `sesiones` |

## Contexto

La corrección [`nombres`](../specs/SPEC-nombres.md) (§2.8 y supuesto 4) dejó los métodos de prueba como `Metodo_Escenario_Resultado`: el primer segmento en inglés (el método probado) y el escenario y el resultado en español, por ejemplo `Rotate_TokenYaRotado_Lanza`. Al mezclar idiomas en un mismo identificador, los nombres se leen mal.

| Punto | Problema | Decisión | Estado |
|---|---|---|---|
| C-01 | Los nombres de las pruebas mezclan inglés y español | Nombre en inglés y `DisplayName` en español | Completado |
| C-02 | `docs/arquitectura.md` §8.1 dice que la regla de nombres es la del SPA | Quitar la referencia: la regla es propia de la API | Completado |

---

## C-01: nombres de las pruebas en inglés, con `DisplayName` en español

**Problema.** 211 métodos de prueba (142 `[Fact]` y 69 `[Theory]`) en 22 archivos tienen el escenario y el resultado en español dentro de un nombre que empieza en inglés.

**Verificación en el código:** ninguna prueba usa `DisplayName`. Las 21 clases de `tests/Matricula.UnitTests` y `HealthCheckTests` de `tests/Matricula.IntegrationTests` (diferida, pero compila con la solución) siguen el formato mixto.

**Decisión** (2026-11-26, decidido por el usuario): el nombre del método va en inglés y un `DisplayName` describe el caso en español.

**Convenciones:**
- **Nombre:** `<MetodoProbado>_<Escenario>_<Resultado>` en inglés, por ejemplo `Rotate_AlreadyRotatedToken_Throws`. Si la prueba no tiene un escenario propio, basta con dos segmentos: `DefaultValues_Page1And20Items`.
- **Nombres del dominio dentro del nombre:** se escriben como en el código, en español, igual que en los nombres combinados de §8.1 (`CursoService`): `CanEnroll_SameCurso_Rejects`, `Deactivate_ActiveAula_MakesItInactive`. Los códigos de regla se escriben como identificador: `Update_InconsistentDates_ThrowsRn16AndKeepsData`.
- **Resultados frecuentes:** `Throws`, `ThrowsArgumentException`, `ReturnsTrue`, `ReturnsFalse`, `ReturnsNull`, `Rejects`, `Allows`, `Keeps…`, `Normalizes`.
- **`DisplayName`:** una oración en español que dice qué pasa en ese caso, sin punto final y con tildes, como los mensajes: `[Fact(DisplayName = "Rotar un token ya rotado lanza InvalidOperationException")]`. En un `[Theory]` describe el conjunto de casos; xUnit agrega los parámetros al mostrarlo.
- **Sin cambios:** los nombres de las clases de prueba (ya siguen §8.1), el cuerpo de las pruebas y sus helpers privados (`Ahora`, `NuevoToken()`…). Esta corrección solo toca el nombre de cada método y su atributo.

**Cambio propuesto:** renombrar los 211 métodos y agregar su `DisplayName`, archivo por archivo. Las pruebas siguen siendo las mismas: 1120 casos en verde antes y después.

**Documentos afectados:**
- `docs/arquitectura.md` §8.1: la fila "Prueba unitaria" de la tabla.
- `AGENTS.md`: la regla de nombres, que hoy pone "las descripciones de las pruebas" en español sin decir dónde van.
- `specs/SPEC-nombres.md`: una línea en el historial que indica que §2.8 y el supuesto 4 quedan reemplazados por esta corrección.
- `specs/mapa-modulos.md`: la decisión 15 remite a esta corrección.

**Estado:** Completado.

---

## C-02: la regla de nombres no menciona el SPA

**Problema.** `docs/arquitectura.md` §8.1 empieza con "La misma regla que el SPA (`SPEC-nombres` de los dos proyectos)". Con C-01 las pruebas de la API dejan de seguir la regla que comparten los dos proyectos.

**Decisión** (2026-11-26, decidido por el usuario): por ahora la arquitectura no hace referencia al SPA; la regla de nombres se describe como propia de la API.

**Cambio propuesto:** quitar esa frase de §8.1 y dejar solo la regla.

**Documentos afectados:** `docs/arquitectura.md` §8.1.

**Estado:** Completado.

---

## Historial

- 2026-11-26: v1 en revisión, con las decisiones del usuario sobre el formato (nombre en inglés y `DisplayName` en español), el registro como corrección y la referencia al SPA.
- 2026-11-26: v1 aprobada por el usuario, con `RefreshTokenTests` como muestra.
- 2026-11-26: corrección cerrada. C-01: los 211 métodos de los 22 archivos tienen nombre en inglés y `DisplayName` en español; 1120 casos en verde, build sin warnings. Detalles:
  - En `TestDatesTests` el primer segmento era `ToLocalDate`, pero esas pruebas llaman a `TestDates.Compute`: pasan a `Compute_…`. La de `DaysOf2028` toma el nombre de su helper.
  - `RegistroErrorTests` usaba `nameof` de su propio método para buscarlo en el stack trace; se actualizó al nombre nuevo.
  - Las specs de módulos cerrados (`SPEC-fundacion`, `SPEC-estudiantes`, `SPEC-profesores`) conservan los nombres viejos en sus ejemplos, como documentos históricos.
  - C-02: §8.1 ya no remite al SPA. Actualizados `docs/arquitectura.md` §8.1, `AGENTS.md`, el historial de `SPEC-nombres`, la decisión 14 del mapa y el índice de correcciones.
