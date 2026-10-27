# Plan: estudiantes

Módulo `estudiantes` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-estudiantes.md`](../../specs/SPEC-estudiantes.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-10-27)

## Enfoque

Igual que `acceso`: primero la base sin endpoints (dominio y persistencia), verificada con pruebas unitarias y con la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http`, Scalar y smtp4dev. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

El registro por administrador va primero (paso 3) porque crea los estudiantes con los que se prueba el resto, incluida la verificación heredada de `acceso` (`403` con un token de estudiante). El registro público llega cuando ya existen estudiantes para probar RN-21. El claim `estudiante_id` y el perfil van juntos, antes de la edición de los datos de contacto, que los necesita.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** entidad `Estudiante` con `ActualizarDatos` y `ActualizarContacto` | — | Solo pruebas unitarias |
| 2 | **Persistencia:** `EstudianteConfiguration`, `DbSet<Estudiante>` y migración `AgregaEstudiante` | 1 | Se verifica con `dotnet ef database update` y `psql` |
| 3 | **Registro por administrador + detalle** (RF-02, RF-17, RN-13, RN-39): `EstudianteService`, DTOs, validador con las reglas nuevas de dirección y fecha de nacimiento, `EstudiantesController`, nombre del estudiante en los correos | 2 | `GET /{id}` va aquí para que el `Location` del `201` sea válido. Verificación heredada de `acceso` |
| 4 | **Listar y editar** (RF-16, RF-18, RN-41, RN-31) | 3 | El cambio de correo pasa por `CuentaService.CambiarCorreoAsync` |
| 5 | **Desactivar y reactivar** (RF-19, RN-38) | 3 | Delegan en `UsuarioService`. Completa el filtro de inactivos del paso 4 y el `409 RN-39` del paso 3 |
| 6 | **Registro público + rate limiting** (RF-01, RN-19, RN-21) | 3 | Reutiliza la regla de contraseña y confirmación de `acceso` |
| 7 | **Claim `estudiante_id` y perfil** | 3 | Toca `DatosToken`, `GeneradorToken`, `AuthService` y `DatosPersonalesResponse` de `acceso` |
| 8 | **Datos de contacto propios** (`PUT /api/v1/me/datos-contacto`) + política `Politicas.Estudiante` | 7 | Identifica al estudiante por el claim |
| 9 | **Cierre del módulo:** `Matricula.Api.http`, README si aplica, estados y verificación heredada en el mapa | todos | Recorrido de la tabla de verificación de la spec |

**Paralelismo:** 4, 5, 6 y 7 dependen solo de 3 y podrían hacerse en cualquier orden; 8 depende de 7. Se hacen en serie, en el orden de la tabla, porque comparten `EstudianteService`, `EstudiantesController` y `Matricula.Api.http`.

## Decisiones de implementación

- **`Estudiante`:** mismo patrón que `Administrador`: constructor privado para EF, constructor público que exige una cuenta con rol `Estudiante`, setters privados y recorte de textos en el dominio. `FechaRegistro` la recibe el constructor (el servicio la calcula con `TimeProvider`), así el dominio no lee la hora.
- **Validación de RN-21 en el registro público:** `EstudianteService` revisa la cédula y el correo **antes** de llamar a `CuentaService.CrearCuentaPublicaAsync` y lanza `BusinessRuleException("RN-21", ...)`. Así no se cambia el contrato de `CuentaService`, que seguiría lanzando `RN-13` por el correo. Es una consulta más por registro; aceptable.
- **Fecha de nacimiento:** regla en `DatosPersonalesRules` que recibe `TimeProvider`; los validadores lo inyectan por constructor (los registra `AddValidatorsFromAssembly`, y `TimeProvider` es singleton).
- **Validadores compartidos:** un validador base o reglas comunes para los datos de registro y edición por administrador, para no repetir las reglas en tres DTOs (registro público, registro por administrador y edición).
- **Proyecciones:** `EstudianteProyecciones.AResponse` (expresión) y `ToResponse()` para la entidad cargada, igual que `AdministradorProyecciones`.
- **Desactivar y reactivar por estudiante:** `EstudianteService` obtiene el `UsuarioId` con una proyección y llama a `UsuarioService`, que sigue siendo el único lugar que cambia el estado (y donde `matriculas` pondrá RN-36).
- **Respuesta del registro público:** `StatusCode(201, new MensajeResponse(...))`, sin `Location`, porque el visitante no puede consultar el recurso (supuesto 4).
- **Token:** `DatosToken` incorpora `int? EstudianteId`. `AuthService.EmitirToken` pasa a ser asíncrono y, solo si el rol es `Estudiante`, busca el id del estudiante por `UsuarioId`. `GeneradorToken` agrega el claim `estudiante_id` cuando viene.
- **Perfil:** `DatosPersonalesResponse` incorpora `FechaNacimiento`, `Direccion` y `FechaRegistro` opcionales; `ObtenerPerfilAsync` toma los datos del administrador o, si no hay, del estudiante, en la misma proyección.
- **Política `Politicas.Estudiante`:** rol `Estudiante` + sin cambio pendiente, como `Politicas.Administrador` (nunca `Roles`).
- **Rate limiting:** política `RegistroEstudiante` en `LimitesPeticiones` (10 cada 15 minutos por IP) con `[EnableRateLimiting]` en la acción del registro público.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Dos registros públicos simultáneos con la misma cédula o correo pasan la validación previa y el segundo choca con el índice único | El `GlobalExceptionHandler` responde `409 RN-13` en lugar de `RN-21`. Es un caso de carrera, poco probable; se acepta |
| Cambiar `EmitirToken` a asíncrono rompe los flujos de `acceso` (login, cambio obligatorio, cambio de contraseña) | Al terminar el paso 7 se repiten a mano el login, el cambio obligatorio y el cambio de contraseña de un administrador y de un estudiante |
| El perfil de `acceso` (`GET /me` de un administrador) cambia de forma al agregar campos | Solo se agregan campos opcionales; el administrador los recibe nulos. Se verifica en el paso 7 |
| El registro público permite averiguar qué cédulas y correos existen (RN-21 obliga a decirlo) | Rate limiting del paso 6 y mensaje sin datos del registro existente (supuesto 6 de la spec) |
| Tokens emitidos antes del paso 7 no llevan `estudiante_id` y el `PUT /me/datos-contacto` los rechaza | El endpoint responde `403` con un mensaje que pide iniciar sesión de nuevo. Solo afecta a la BD de desarrollo |
| La fecha de nacimiento se valida con la fecha del servidor en UTC y rechaza a alguien nacido "hoy" en otra zona horaria | Se compara con `DateOnly` de `TimeProvider` en UTC; el desfase es de horas y no afecta casos reales |
| El orden de las rutas `estudiantes/registro` y `estudiantes/{id}` choca | `{id:int}` en las rutas con id; `registro` es literal |

## Puntos de verificación

- **Tras 2:** `\d estudiante` en `psql` muestra los índices únicos en `cedula` y `usuario_id` y la FK a `usuario` con `ON DELETE RESTRICT`.
- **Tras 3:** un estudiante registrado por el administrador recibe la temporal con su nombre, hace el cambio obligatorio y con su token un endpoint de administrador responde `403` (heredada de `acceso`).
- **Tras 5:** ciclo completo del administrador sobre un estudiante: registrar, listar, editar, desactivar (token `401`, login `403 RN-29`) y reactivar.
- **Tras 7:** login, cambio obligatorio y cambio de contraseña siguen funcionando para administradores y estudiantes; el token del estudiante trae `estudiante_id`.
- **Final:** toda la tabla de verificación manual de la spec; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Eliminar estudiantes y RN-36 → `matriculas`. RF-20, RF-21 y RF-54 → `matriculas`. Pruebas de integración → diferidas.
