# Plan: profesores

Módulo `profesores` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-profesores.md`](../../specs/SPEC-profesores.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-10-28)

## Enfoque

Igual que `estudiantes`: primero la base sin endpoints (dominio y persistencia), verificada con pruebas unitarias y con la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http`, Scalar y smtp4dev. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

El registro va primero (paso 3) porque crea los profesores con los que se prueba el resto. El claim `profesor_id` y el perfil van al final porque tocan código compartido de `acceso` (`AuthService`, `DatosToken`, `GeneradorToken`) y conviene hacerlos con el resto del módulo ya estable, repitiendo la regresión de login de los tres roles.

Es un módulo más corto que `estudiantes`: no hay registro público, rate limiting ni edición de datos propios.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** entidad `Profesor` con `ActualizarDatos` | — | Solo pruebas unitarias |
| 2 | **Persistencia:** `ProfesorConfiguration`, `DbSet<Profesor>` y migración `AgregaProfesor` | 1 | Se verifica con `dotnet ef database update` y `psql` |
| 3 | **Registro + detalle** (RF-03, RF-22, RF-24, RN-13, RN-20, RN-39): `ProfesorService`, DTOs, validador con las reglas nuevas de especialidad y fecha de contratación, `ProfesoresController`, nombre del profesor en los correos | 2 | `GET /{id}` va aquí para que el `Location` del `201` sea válido |
| 4 | **Listar y editar** (RF-23, RF-25, RN-41, RN-31) | 3 | El cambio de correo pasa por `CuentaService.CambiarCorreoAsync` |
| 5 | **Desactivar y reactivar** (RF-26, RN-38) | 3 | Delegan en `UsuarioService`. Completa el filtro de inactivos del paso 4 y el `409 RN-39` del paso 3 |
| 6 | **Claim `profesor_id` y perfil** | 3 | Toca `DatosToken`, `GeneradorToken`, `AuthService` y `DatosPersonalesResponse` |
| 7 | **Cierre del módulo:** `Matricula.Api.http`, README si aplica, estados en la spec, el mapa y la lista | todos | Recorrido de la tabla de verificación de la spec |

**Paralelismo:** 4, 5 y 6 dependen solo de 3 y podrían hacerse en cualquier orden. Se hacen en serie, en el orden de la tabla, porque comparten `ProfesorService`, `ProfesoresController` y `Matricula.Api.http`.

## Decisiones de implementación

- **`Profesor`:** mismo patrón que `Estudiante`: constructor privado para EF, constructor público `(usuario, cedula, nombre, apellidos, telefono, especialidad, fechaContratacion)` que exige una cuenta con rol `Profesor`, setters privados y recorte de textos en el dominio. Un solo método `ActualizarDatos`, porque el profesor no edita sus datos.
- **Validación:** `DatosPersonalesRules` incorpora `Especialidad` (hasta 100 caracteres) y `FechaContratacion` (no anterior a 1900-01-01). Esta última no necesita `TimeProvider`, porque acepta fechas futuras (supuesto 3 de la spec). El validador de edición reutiliza las mismas reglas que el de registro, como en estudiantes.
- **Proyecciones:** `ProfesorProyecciones.AResponse` (expresión) y `ToResponse()` para la entidad cargada, igual que `EstudianteProyecciones`.
- **RN-13 / RN-39 en el registro:** antes de crear la cuenta, `ProfesorService` busca la cédula en `Profesores` incluyendo `Usuario.Activo`: activo → `409 RN-13`; inactivo → `409 RN-39` con `profesorId` y `usuarioId`. El correo repetido lo rechaza `CuentaService` con `RN-13`, igual que en estudiantes.
- **Desactivar y reactivar:** `ProfesorService` obtiene el `UsuarioId` con una proyección y llama a `UsuarioService`, que sigue siendo el único lugar que cambia el estado (y donde `grupos` pondrá RN-37).
- **Correos:** `EnvioCorreos.ObtenerNombreAsync` agrega un tercer `?? await db.Profesores...` y se quita el comentario "El módulo profesores añade aquí…".
- **Token:** `DatosToken` incorpora `int? ProfesorId` como último parámetro. `AuthService.EmitirTokenAsync` ya es asíncrono; si el rol es `Profesor`, busca el id del profesor por `UsuarioId`, igual que hace con el estudiante. `GeneradorToken` agrega el claim `profesor_id` cuando viene.
- **Perfil:** `DatosPersonalesResponse` incorpora `Especialidad` y `FechaContratacion` opcionales al final. `ObtenerPerfilAsync` agrega la subconsulta `Profesor` en la misma proyección y devuelve `Administrador ?? Estudiante ?? Profesor`. Las proyecciones existentes de administrador y estudiante pasan `null` en los campos nuevos.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Agregar parámetros a `DatosToken` y `DatosPersonalesResponse` rompe sus usos actuales | Son records posicionales: el compilador marca cada uso. Se actualizan todos en la misma tarea (paso 6) |
| El cambio en `AuthService` rompe el login o el perfil de administradores y estudiantes | Al terminar el paso 6 se repiten a mano el login, el cambio obligatorio, el cambio de contraseña y `GET /me` de los tres roles |
| La subconsulta adicional del perfil hace más lenta `GET /me` | Son tres subconsultas por índice único (`usuario_id`); el costo es despreciable |
| Dos registros simultáneos con la misma cédula pasan la validación previa | El índice único lo impide y el `GlobalExceptionHandler` responde `409 RN-13`. Caso de carrera poco probable; se acepta, igual que en estudiantes |
| Tokens de profesor emitidos antes del paso 6 no llevan `profesor_id` | Ningún endpoint de este módulo lo usa; `grupos` lo necesitará y para entonces los tokens ya se habrán renovado. Solo afecta a la BD de desarrollo |
| El profesor queda sin forma de ver sus grupos en este módulo | Es lo esperado: RF-27 y RF-28 son de `grupos` (spec, fuera de alcance) |

## Puntos de verificación

- **Tras 2:** `\d profesor` en `psql` muestra los índices únicos en `cedula` y `usuario_id` y la FK a `usuario` con `ON DELETE RESTRICT`.
- **Tras 3:** un profesor registrado recibe la temporal con su nombre, hace el cambio obligatorio y con su token un endpoint de administrador responde `403`.
- **Tras 5:** ciclo completo del administrador sobre un profesor: registrar, listar, editar, desactivar (token `401`, login `403 RN-29`) y reactivar.
- **Tras 6:** login, cambio obligatorio, cambio de contraseña y `GET /me` siguen funcionando para los tres roles; el token del profesor trae `profesor_id`.
- **Final:** toda la tabla de verificación manual de la spec; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Eliminar profesores y RN-37 → `grupos` (decisión 1 de la spec). RF-27, RF-28 y `Politicas.Profesor` → `grupos`. Verificación heredada de kebab-case → primer módulo con un controller de nombre compuesto (decisión 2 de la spec). Pruebas de integración → diferidas.
