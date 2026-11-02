# Tareas: profesores

Módulo `profesores` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-profesores.md`](../../specs/SPEC-profesores.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-10-28)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio `Profesor` | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Registro y detalle | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T4. Listar y editar profesores | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T5. Desactivar y reactivar | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T6. Claim `profesor_id` y perfil | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T7. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Ninguna se copia a este módulo. La de `fundacion` T3 (ruta en kebab-case de un controller de nombre compuesto) sigue pendiente en el mapa: `ProfesoresController` no tiene nombre compuesto (decisión 2 de la spec).

## Detalle

### T1. Dominio `Profesor`
- Acceptance:
  - `Profesor : EntidadBase` con `Cedula`, `Nombre`, `Apellidos`, `Telefono`, `Especialidad?`, `FechaContratacion?`, `UsuarioId` y navegación a `Usuario`. Sin correo ni estado (decisiones 2 y 7 del mapa).
  - Constructor privado para EF y constructor público `(usuario, cedula, nombre, apellidos, telefono, especialidad, fechaContratacion)` que exige una cuenta con rol `Profesor`.
  - `ActualizarDatos(cedula, nombre, apellidos, telefono, especialidad, fechaContratacion)`. Recorta los textos; la especialidad vacía o en blanco queda `null`.
- Verify: `dotnet test tests/Matricula.UnitTests` con `ProfesorTests`: rechaza una cuenta de otro rol, recorta los textos, especialidad en blanco → `null`, `ActualizarDatos` cambia todos los datos personales.
- Files: `src/Matricula.Domain/Entities/Profesor.cs`, `tests/Matricula.UnitTests/Domain/ProfesorTests.cs`

### T2. Persistencia y migración
- Acceptance:
  - `ProfesorConfiguration`: longitudes (`cedula` 20, `nombre` y `apellidos` 100, `telefono` 20, `especialidad` 100), índices únicos en `cedula` y `usuario_id`, FK a `usuario` con `Restrict`.
  - `DbSet<Profesor>` en `IAppDbContext` y `AppDbContext`.
  - Migración `AgregaProfesor`.
- Verify: `dotnet ef database update`; en `psql`, `\d profesor` muestra los índices únicos y la FK con `ON DELETE RESTRICT`.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/ProfesorConfiguration.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Registro y detalle
- Acceptance:
  - `DatosPersonalesRules`: reglas nuevas `Especialidad` (hasta 100 caracteres) y `FechaContratacion` (no anterior a 1900-01-01; se aceptan fechas futuras).
  - `RegistrarProfesorRequest` + validador; `ProfesorResponse`, `ProfesorRegistradoResponse` (`profesor` + `correoEnviado`) y `ProfesorProyecciones`.
  - `ProfesorService.RegistrarAsync`: cédula de un profesor activo → `409 RN-13`; de uno inactivo → `409 RN-39` con `profesorId` y `usuarioId`; cuenta con `CuentaService.CrearCuentaConTemporalAsync(…, Rol.Profesor, …)`, un solo `SaveChangesAsync` y después la temporal.
  - `ProfesorService.ObtenerAsync` → `200` o `404`.
  - `EnvioCorreos.ObtenerNombreAsync` también busca en `Profesores`.
  - `ProfesoresController` con `POST /api/v1/profesores` (`201` + `Location`) y `GET /api/v1/profesores/{id:int}`, ambos con `Politicas.Administrador`. `ProfesorService` registrado en `AddApplication`.
- Verify:
  - Registro válido → `201` + `Location`; temporal en smtp4dev con el nombre del profesor; la respuesta no trae la contraseña.
  - Correo existente con otra capitalización → `409 RN-13`; cédula de un profesor activo → `409 RN-13`.
  - Cédula de un estudiante o administrador existente, con otro correo → `201` (RN-13 es por tabla).
  - Cédula vacía, especialidad de 101 caracteres, fecha de contratación de 1899 o cuerpo vacío → `400` en español.
  - Detalle de un id inexistente → `404`.
  - El profesor entra con la temporal (`debeCambiarContrasena: true`), hace el cambio obligatorio y accede normalmente con rol `Profesor`.
  - `GET` y `POST /api/v1/profesores` con el token del profesor o de un estudiante → `403`; sin token → `401`.
  - Se verifica en T5 (necesita un profesor inactivo): cédula de un inactivo → `409 RN-39` con `profesorId` y `usuarioId`.
- Files: `src/Matricula.Application/Common/Validacion/DatosPersonalesRules.cs`, `src/Matricula.Application/Profesores/{ProfesorService,RegistrarProfesorRequest,RegistrarProfesorRequestValidator,ProfesorResponse,ProfesorRegistradoResponse,ProfesorProyecciones}.cs`, `src/Matricula.Application/Correos/EnvioCorreos.cs`, `src/Matricula.Api/Controllers/ProfesoresController.cs`, `src/Matricula.Application/DependencyInjection.cs`

### T4. Listar y editar profesores
- Acceptance:
  - `ListarProfesoresRequest` (hereda `PaginacionRequest`, con `Search` e `IncluirInactivos`); `ProfesorService.ListarAsync`: solo activos salvo `incluirInactivos` (RN-41), búsqueda en nombre, apellidos, cédula, especialidad y correo, orden por apellidos, nombre e id, proyección con `Select` y `AsNoTracking`.
  - `ActualizarProfesorRequest` + validador (mismas reglas que el registro); `ProfesorService.ActualizarAsync`: cédula de otro profesor → `409 RN-13`; correo con `CuentaService.CambiarCorreoAsync` (RN-31); `ActualizarDatos`; un solo `SaveChangesAsync`.
  - `GET /api/v1/profesores` y `PUT /api/v1/profesores/{id:int}` con `Politicas.Administrador`.
- Verify:
  - El listado pagina, ordena por apellidos y busca por nombre, cédula, especialidad y correo. El filtro de inactivos se verifica en T5.
  - Editar datos → `200`; el detalle los muestra. Borrar la especialidad (vacía) la deja en `null`.
  - Cédula de otro profesor o correo de otra cuenta → `409 RN-13`; id inexistente → `404`.
  - Cambiar el correo → el login funciona con el nuevo y no con el anterior.
- Files: `src/Matricula.Application/Profesores/{ProfesorService,ListarProfesoresRequest,ActualizarProfesorRequest,ActualizarProfesorRequestValidator}.cs`, `ProfesoresController.cs`

### T5. Desactivar y reactivar
- Acceptance:
  - `ProfesorService.DesactivarAsync` y `ReactivarAsync`: obtienen el `UsuarioId` del profesor (`404` si no existe) y llaman a `UsuarioService.DesactivarAsync` / `ReactivarAsync`.
  - `POST /api/v1/profesores/{id:int}/desactivar` y `.../reactivar` con `Politicas.Administrador`, `204`.
- Verify:
  - Desactivar un profesor con sesión abierta → su token `401`; su login `403 RN-29`; el detalle muestra `activo: false`.
  - Reactivar → vuelve a iniciar sesión; el detalle muestra `activo: true`.
  - Desactivarlo por `/api/v1/usuarios/{usuarioId}/desactivar` tiene el mismo efecto en el detalle del profesor (RN-38).
  - Pendiente de T3: registrar con la cédula de un inactivo → `409 RN-39` con `profesorId` y `usuarioId`.
  - Pendiente de T4: el listado oculta al inactivo sin `incluirInactivos` y lo muestra con `incluirInactivos=true`.
  - ⏳ La guarda RN-37 (grupos activos) la añade `grupos`.
- Files: `src/Matricula.Application/Profesores/ProfesorService.cs`, `ProfesoresController.cs`

### T6. Claim `profesor_id` y perfil
- Acceptance:
  - `DatosToken` incorpora `int? ProfesorId`; `GeneradorToken` agrega el claim `profesor_id` cuando viene.
  - `AuthService.EmitirTokenAsync`: si el rol es `Profesor`, busca el id del profesor por `UsuarioId`. Lo usan el login, el cambio obligatorio y el cambio de contraseña.
  - `DatosPersonalesResponse` incorpora `Especialidad` y `FechaContratacion` opcionales. `ObtenerPerfilAsync` devuelve los datos del administrador, del estudiante o del profesor.
- Verify:
  - Login de un profesor → el token (decodificado en jwt.io o similar) trae `profesor_id` y no `estudiante_id`; el de un estudiante trae `estudiante_id` y no `profesor_id`; el de un administrador, ninguno.
  - `GET /api/v1/me` como profesor → cuenta y datos personales con especialidad y fecha de contratación; como administrador y como estudiante → igual que antes, con esos campos en `null`.
  - Regresión de `acceso` y `estudiantes`: login, cambio obligatorio y cambio de contraseña de los tres roles siguen funcionando; `PUT /api/v1/me/datos-contacto` del estudiante sigue respondiendo `200`.
- Files: `src/Matricula.Application/Abstractions/DatosToken.cs`, `src/Matricula.Api/Seguridad/GeneradorToken.cs`, `src/Matricula.Application/Auth/{AuthService,DatosPersonalesResponse}.cs`

### T7. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - README: paso de la migración `AgregaProfesor` si hace falta mencionarlo.
  - Recorrido de la tabla de verificación manual de la spec; consola revisada sin contraseñas ni temporales.
  - Estados de la spec, el mapa y esta lista al día.
- Verify: revisión manual del README y de los estados; prueba de humo final con datos nuevos sobre toda la tabla de verificación de la spec.
- Files: `README.md`, `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`, `specs/SPEC-profesores.md`, `tasks/profesores/todo.md`
