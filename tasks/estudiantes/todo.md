# Tareas: estudiantes

Módulo `estudiantes` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-estudiantes.md`](../../specs/SPEC-estudiantes.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-10-27)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio `Estudiante` | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Registro por administrador y detalle | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Listar y editar estudiantes | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Desactivar y reactivar | Completada | ✅ | ➖ | ✅ | ✅ |
| T6. Registro público y rate limiting | Completada | ✅ | ➖ | ✅ | ✅ |
| T7. Claim `estudiante_id` y perfil | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T8. Datos de contacto propios | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T9. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Copiadas de `specs/mapa-modulos.md` ("Verificaciones heredadas"):

| Origen | Verificación | Tarea |
|---|---|---|
| `acceso` T10 | Un endpoint de administrador (por ejemplo, `POST /api/v1/administradores`) con el token de un estudiante responde `403`, no `401` ni `200` | T3 ✅ |

Al completarlas se marcan como cumplidas en el mapa.

## Detalle

### T1. Dominio `Estudiante`
- Acceptance:
  - `Estudiante : EntidadBase` con `Cedula`, `Nombre`, `Apellidos`, `Telefono`, `FechaNacimiento?`, `Direccion?`, `FechaRegistro`, `UsuarioId` y navegación a `Usuario`. Sin correo ni estado (decisiones 2 y 7 del mapa).
  - Constructor privado para EF y constructor público `(usuario, cedula, nombre, apellidos, telefono, fechaNacimiento, direccion, fechaRegistro)` que exige una cuenta con rol `Estudiante`.
  - `ActualizarDatos(cedula, nombre, apellidos, telefono, fechaNacimiento, direccion)` y `ActualizarContacto(telefono, direccion)`. Recortan los textos; la dirección vacía o en blanco queda `null`.
- Verify: `dotnet test tests/Matricula.UnitTests` con `EstudianteTests`: rechaza una cuenta de otro rol, recorta los textos, dirección en blanco → `null`, `ActualizarContacto` solo cambia teléfono y dirección, `ActualizarDatos` cambia los datos personales y conserva `FechaRegistro`.
- Files: `src/Matricula.Domain/Entities/Estudiante.cs`, `tests/Matricula.UnitTests/Domain/EstudianteTests.cs`

### T2. Persistencia y migración
- Acceptance:
  - `EstudianteConfiguration`: longitudes (`cedula` 20, `nombre` y `apellidos` 100, `telefono` 20, `direccion` 250), índices únicos en `cedula` y `usuario_id`, FK a `usuario`.
  - `DbSet<Estudiante>` en `IAppDbContext` y `AppDbContext`.
  - Migración `AgregaEstudiante`.
- Verify: `dotnet ef database update`; en `psql`, `\d estudiante` muestra los índices únicos y la FK con `ON DELETE RESTRICT`.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/EstudianteConfiguration.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Registro por administrador y detalle
- Acceptance:
  - `DatosPersonalesRules`: reglas nuevas `Direccion` (hasta 250 caracteres) y `FechaNacimiento` (no futura ni anterior a 1900-01-01, con `TimeProvider`).
  - `RegistrarEstudianteRequest` + validador; `EstudianteResponse`, `EstudianteRegistradoResponse` (`estudiante` + `correoEnviado`) y `EstudianteProyecciones`.
  - `EstudianteService.RegistrarAsync`: cédula de un estudiante activo → `409 RN-13`; de uno inactivo → `409 RN-39` con `estudianteId` y `usuarioId`; cuenta con `CuentaService.CrearCuentaConTemporalAsync(…, Rol.Estudiante, …)`, `FechaRegistro` de hoy, un solo `SaveChangesAsync` y después la temporal.
  - `EstudianteService.ObtenerAsync` → `200` o `404`.
  - `EnvioCorreos.ObtenerNombreAsync` también busca en `Estudiantes`.
  - `EstudiantesController` con `POST /api/v1/estudiantes` (`201` + `Location`) y `GET /api/v1/estudiantes/{id:int}`, ambos con `Politicas.Administrador`. `EstudianteService` registrado en `AddApplication`.
- Verify:
  - Registro válido → `201` + `Location`; temporal en smtp4dev con el nombre del estudiante; la respuesta no trae la contraseña.
  - Correo existente con otra capitalización → `409 RN-13`; cédula de un estudiante activo → `409 RN-13`.
  - Fecha de nacimiento futura, dirección de más de 250 caracteres o cuerpo vacío → `400` en español.
  - Detalle de un id inexistente → `404`.
  - El estudiante entra con la temporal, hace el cambio obligatorio y accede normalmente.
  - ✅ **Heredada:** `POST /api/v1/administradores` con el token del estudiante → `403`, no `401` ni `200`. También `GET` y `POST /api/v1/estudiantes` → `403`; sin token → `401`.
  - Se verifica en T5 (necesita un estudiante inactivo): cédula de un inactivo → `409 RN-39` con `estudianteId` y `usuarioId`.
- Files: `src/Matricula.Application/Common/Validacion/DatosPersonalesRules.cs`, `src/Matricula.Application/Estudiantes/{EstudianteService,RegistrarEstudianteRequest,RegistrarEstudianteRequestValidator,EstudianteResponse,EstudianteRegistradoResponse,EstudianteProyecciones}.cs`, `src/Matricula.Application/Correos/EnvioCorreos.cs`, `src/Matricula.Api/Controllers/EstudiantesController.cs`, `src/Matricula.Application/DependencyInjection.cs`

### T4. Listar y editar estudiantes
- Acceptance:
  - `ListarEstudiantesRequest` (hereda `PaginacionRequest`, con `Search` e `IncluirInactivos`); `EstudianteService.ListarAsync`: solo activos salvo `incluirInactivos` (RN-41), búsqueda en nombre, apellidos, cédula y correo, orden por apellidos, nombre e id, proyección con `Select` y `AsNoTracking`.
  - `ActualizarEstudianteRequest` + validador (mismas reglas que el registro por administrador); `EstudianteService.ActualizarAsync`: cédula de otro estudiante → `409 RN-13`; correo con `CuentaService.CambiarCorreoAsync` (RN-31); `ActualizarDatos`; un solo `SaveChangesAsync`.
  - `GET /api/v1/estudiantes` y `PUT /api/v1/estudiantes/{id:int}` con `Politicas.Administrador`.
- Verify:
  - El listado pagina, ordena por apellidos y busca por nombre, cédula y correo. El filtro de inactivos se verifica en T5.
  - Editar datos → `200`; el detalle los muestra y `FechaRegistro` no cambia.
  - Cédula de otro estudiante o correo de otra cuenta → `409 RN-13`; id inexistente → `404`.
  - Cambiar el correo → el login funciona con el nuevo y no con el anterior.
- Files: `src/Matricula.Application/Estudiantes/{EstudianteService,ListarEstudiantesRequest,ActualizarEstudianteRequest,ActualizarEstudianteRequestValidator}.cs`, `EstudiantesController.cs`

### T5. Desactivar y reactivar
- Acceptance:
  - `EstudianteService.DesactivarAsync` y `ReactivarAsync`: obtienen el `UsuarioId` del estudiante (`404` si no existe) y llaman a `UsuarioService.DesactivarAsync` / `ReactivarAsync`.
  - `POST /api/v1/estudiantes/{id:int}/desactivar` y `.../reactivar` con `Politicas.Administrador`, `204`.
- Verify:
  - Desactivar un estudiante con sesión abierta → su token `401`; su login `403 RN-29`; el detalle muestra `activo: false`.
  - Reactivar → vuelve a iniciar sesión; el detalle muestra `activo: true`.
  - Desactivarlo por `/api/v1/usuarios/{usuarioId}/desactivar` tiene el mismo efecto en el detalle del estudiante (RN-38).
  - ✅ Pendiente de T3: registrar con la cédula de un inactivo → `409 RN-39` con `estudianteId` y `usuarioId`.
  - ✅ Pendiente de T4: el listado oculta al inactivo sin `incluirInactivos` y lo muestra con `incluirInactivos=true`.
- Files: `src/Matricula.Application/Estudiantes/EstudianteService.cs`, `EstudiantesController.cs`

### T6. Registro público y rate limiting
- Acceptance:
  - `RegistroPublicoRequest` (datos del estudiante + `contrasena` y `confirmacion`, sin rol) + validador que reutiliza `DatosPersonalesRules` y la regla de contraseña y confirmación de `acceso` (RN-26).
  - `EstudianteService.RegistrarPublicoAsync`: cédula de cualquier estudiante o correo de cualquier cuenta → `409 RN-21` con el mensaje de iniciar sesión, solicitar una temporal por olvido o contactar a la administración, sin datos del registro existente; cuenta con `CuentaService.CrearCuentaPublicaAsync` (RN-19); un solo `SaveChangesAsync`; sin correo.
  - `POST /api/v1/estudiantes/registro` con `[AllowAnonymous]` y `[EnableRateLimiting]`; responde `201` con un `MensajeResponse`, sin `Location`.
  - Política `RegistroEstudiante` en `LimitesPeticiones`: 10 cada 15 minutos por IP.
- Verify:
  - Registro válido → `201`; login con esa contraseña → `200`, rol `Estudiante`, `debeCambiarContrasena: false`.
  - Un campo `rol: "Administrador"` en el cuerpo se ignora; la cuenta queda como `Estudiante` (RN-19).
  - Contraseña débil o confirmación distinta → `400`.
  - Cédula existente (activo e inactivo) o correo existente con otra capitalización → `409 RN-21` sin ids.
  - 11 registros en 15 minutos desde la misma IP → el último `429`.
- Files: `src/Matricula.Application/Estudiantes/{EstudianteService,RegistroPublicoRequest,RegistroPublicoRequestValidator}.cs`, `EstudiantesController.cs`, `src/Matricula.Api/Seguridad/LimitesPeticiones.cs`

### T7. Claim `estudiante_id` y perfil
- Acceptance:
  - `DatosToken` incorpora `int? EstudianteId`; `GeneradorToken` agrega el claim `estudiante_id` cuando viene.
  - `AuthService.EmitirToken` pasa a ser asíncrono y, si el rol es `Estudiante`, busca el id del estudiante por `UsuarioId`. Lo usan el login, el cambio obligatorio y el cambio de contraseña.
  - `DatosPersonalesResponse` incorpora `FechaNacimiento`, `Direccion` y `FechaRegistro` opcionales. `ObtenerPerfilAsync` devuelve los datos del administrador o, si no hay, los del estudiante.
- Verify:
  - Login de un estudiante → el token (decodificado en jwt.io o similar) trae `estudiante_id`; el de un administrador no.
  - `GET /api/v1/me` como estudiante → cuenta y datos personales con fecha de nacimiento, dirección y fecha de registro; como administrador → igual que antes, con esos campos en `null`.
  - Regresión de `acceso`: login, cambio obligatorio y cambio de contraseña de un administrador y de un estudiante siguen funcionando.
- Files: `src/Matricula.Application/Abstractions/DatosToken.cs`, `src/Matricula.Api/Seguridad/GeneradorToken.cs`, `src/Matricula.Application/Auth/{AuthService,DatosPersonalesResponse}.cs`

### T8. Datos de contacto propios
- Acceptance:
  - Política `Politicas.Estudiante`: rol `Estudiante` + sin cambio pendiente (RN-24), registrada en `AddPoliticas`.
  - `ActualizarContactoRequest` (`telefono`, `direccion?`) + validador.
  - `EstudianteService.ActualizarContactoAsync`: identifica al estudiante con `ICurrentUser.EstudianteId` (sin el claim → `403` con un mensaje que pide iniciar sesión de nuevo), `ActualizarContacto` y guarda. Devuelve el perfil (`AuthService.ObtenerPerfilAsync`).
  - `PUT /api/v1/me/datos-contacto` en `MeController` con `Politicas.Estudiante`.
- Verify:
  - Como estudiante → `200` con el perfil actualizado; `GET /api/v1/me` lo muestra.
  - Teléfono inválido → `400`; un campo `correo` o `cedula` en el cuerpo se ignora (decisiones 1 y 2 de la spec).
  - Como administrador → `403`; sin token → `401`.
- Files: `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Application/Estudiantes/{EstudianteService,ActualizarContactoRequest,ActualizarContactoRequestValidator}.cs`, `src/Matricula.Api/Controllers/MeController.cs`

### T9. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - README: paso de la migración `AgregaEstudiante` si hace falta mencionarlo y la sección de rate limiting con la nueva política.
  - Recorrido de la tabla de verificación manual de la spec; consola revisada sin contraseñas ni temporales.
  - Verificación heredada marcada como cumplida en `specs/mapa-modulos.md`; estados de la spec, el mapa y esta lista al día.
- Verify: revisión manual del README y de los estados.
- Files: `README.md`, `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`, `specs/SPEC-estudiantes.md`, `tasks/estudiantes/todo.md`
