# Spec: acceso

Módulo `acceso` del [mapa de módulos](mapa-modulos.md). Depende de `fundacion`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/acceso/plan.md) | [Aprobadas](../tasks/acceso/todo.md) | 9/13 tareas completadas |

**Historial:**
- 2026-10-19: v1 en revisión.
- 2026-10-19: v2 en revisión. Los administradores tienen datos personales (entidad `Administrador`, RF-55), la cédula es única por tabla, el correo vive solo en `Usuario` y el seed crea al administrador inicial con datos genéricos. Decisiones 6 y 7 del mapa. Se quitan el listado y detalle genéricos de `/usuarios`.
- 2026-10-19: v3 en revisión. Dos endpoints para cambiar la contraseña y el rol de una cuenta no cambia (decisión 8 del mapa). Sin preguntas abiertas.
- 2026-10-19: v3 aprobada.
- 2026-10-19: T6, los endpoints de administrador usan la política `Politicas.Administrador` en lugar de `Roles`, y se documenta que una ruta inexistente sin token responde `401` (supuesto 11).
- 2026-10-19: el seed se ejecuta al inicio de `Program` en lugar de en un `IHostedService` (sin paquetes nuevos; §5 de la arquitectura admite ambas opciones).

## Objetivo

Dar al sistema cuentas de usuario, autenticación y autorización, para que los módulos siguientes solo tengan que declarar quién puede usar cada endpoint, y registrar a los administradores con sus datos personales. Cubre RF-04 a RF-14 y RF-55, y las reglas RN-13 (correo y cédula de administradores), RN-20, RN-22 a RN-33 y RN-39 (administradores).

**Usuarios:**
- Visitante: inicia sesión y solicita una temporal por olvido.
- Cualquier usuario con sesión: cambia su contraseña, consulta su perfil y cierra sesión.
- Administrador: registra, consulta y edita administradores; reenvía temporales; activa o desactiva cuentas.
- El equipo de desarrollo: los módulos `estudiantes` y `profesores` crean sus cuentas con el servicio que expone este módulo.

### Alcance

1. **Entidad `Usuario`** (§5.1 de los requerimientos y §7 de la arquitectura).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id`, `FechaCreacion`, `FechaModificacion` | de `EntidadBase` | |
   | `Correo` | `string(256)` | Normalizado (sin espacios y en minúsculas). Índice único (RN-13). Es también el correo de contacto de la persona: `Administrador`, `Estudiante` y `Profesor` no lo duplican (decisión 7 del mapa) |
   | `PasswordHash` | `string?` | Vacío en cuentas creadas por un administrador hasta su primer ingreso (RN-22) |
   | `TempPasswordHash` | `string?` | Como máximo una temporal vigente (RN-27) |
   | `TempPasswordExpira` | `DateTimeOffset?` | |
   | `TempPasswordMotivo` | `MotivoTemporal?` | `CuentaNueva`, `Olvido` o `ReenvioAdministrador` |
   | `Rol` | `Rol` | `Administrador`, `Profesor` o `Estudiante`. Propiedad `init`: se fija al crear y nunca cambia (RN-31, decisión 8 del mapa) |
   | `Activo` | `bool` | Estado único de la persona (decisión 2 del mapa) |
   | `SecurityStamp` | `string(36)` | Se regenera al cambiar la contraseña, desactivar la cuenta o cerrar sesión |
   | `OrigenRegistro` | `OrigenRegistro` | `RegistroPublico` o `Administrador` |
   | `CreadoPorId` | `int?` | FK a `Usuario`, `Restrict`. Nulo en el registro público y en el administrador inicial |
   | `UltimoIngreso` | `DateTimeOffset?` | |
   | `FechaCambioContrasena` | `DateTimeOffset?` | |

   Las reglas que solo dependen del propio usuario son métodos de la entidad: asignar una temporal con su vencimiento (RN-23, RN-27), saber si la temporal está vigente, establecer la contraseña definitiva, registrar un ingreso con la definitiva (anula la temporal, RF-11), desactivar y reactivar. La política de contraseñas (RN-26) es una regla pura en Domain, reutilizada por los validadores.

2. **Entidad `Administrador`** (§5.4 de los requerimientos, decisión 6 del mapa).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id`, `FechaCreacion`, `FechaModificacion` | de `EntidadBase` | |
   | `Cedula` | `string(20)` | Índice único en la tabla `administrador` (RN-13, única por tabla) |
   | `Nombre` | `string(100)` | |
   | `Apellidos` | `string(100)` | |
   | `Telefono` | `string(20)` | |
   | `UsuarioId` | `int` | FK a `Usuario`, `Restrict`, índice único (1:1, RN-31) |

   Sin `Correo` ni `Activo`: se leen de su `Usuario`. Una sola migración, `AgregaUsuarioYAdministrador`, crea las dos tablas.

3. **Autenticación JWT** (§7 de la arquitectura)
   - Token HS256 de 60 minutos, sin refresh token. Claims: id del usuario (`NameIdentifier`), correo, rol, `security_stamp` y, si aplica, `must_change_password=true`.
   - `JwtOptions` (`Issuer`, `Audience`, `Clave`, `MinutosVigencia`) con `ValidateOnStart`. La `Clave` va en user-secrets y debe tener al menos 32 caracteres.
   - `OnTokenValidated` lee de la BD el `SecurityStamp` y el `Activo` del usuario; si el stamp no coincide o la cuenta está inactiva, el token se rechaza (`401`). Así funcionan el logout, la desactivación (RN-29) y el cambio de contraseña.
   - Hash de contraseñas y temporales con `PasswordHasher<Usuario>`, detrás de una interfaz de Application.

4. **Autorización**
   - Política de respaldo (`FallbackPolicy`) y por defecto: usuario autenticado **y sin** el claim `must_change_password` (RN-24). Los endpoints públicos llevan `[AllowAnonymous]`; también `/health`, OpenAPI y Scalar.
   - Política `PasswordChangePending`: solo exige autenticación. La usan únicamente el cambio obligatorio y el logout.
   - Roles con la política `[Authorize(Policy = Politicas.Administrador)]` (rol + sin cambio pendiente). **No** se usa `[Authorize(Roles = ...)]`: reemplaza la política por defecto y dejaría pasar a un administrador con cambio pendiente (RN-24). Los módulos siguientes siguen la misma regla.
   - Un usuario con cambio pendiente que llama a otro endpoint recibe `403` con `code: "RN-24"`.
   - Esquema Bearer en el documento OpenAPI, para probar con token desde Scalar.

5. **Endpoints**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/auth/login` | Anónimo, con rate limiting | RF-06, RF-07, RF-11, RN-27, RN-29 | `200` con `{ accessToken, expiraEn, rol, debeCambiarContrasena }` · `401` credenciales inválidas |
   | `POST /api/v1/auth/cambio-obligatorio` | `PasswordChangePending` | RF-07, RN-25, RN-26, RN-33 | `200` con un token nuevo, sin el claim · `403` si no hay cambio pendiente |
   | `POST /api/v1/auth/cambiar-contrasena` | Autenticado | RF-09, RN-25, RN-26, RN-33 | `200` con un token nuevo |
   | `POST /api/v1/auth/logout` | `PasswordChangePending` | RF-08 | `204` |
   | `POST /api/v1/auth/olvido-contrasena` | Anónimo, con rate limiting | RF-10, RN-28, RN-29, RN-32 | `200` siempre, con el mismo mensaje |
   | `GET /api/v1/me` | Autenticado | RF-13 | `200` con los datos de la cuenta (correo, rol, estado, fecha de creación, último ingreso, último cambio de contraseña) y, para un administrador, sus datos personales |
   | `POST /api/v1/administradores` | Administrador | RF-04, RF-05, RN-13, RN-20, RN-22, RN-39 | `201` + `Location` con el administrador y `correoEnviado` · `409 RN-13` · `409 RN-39` |
   | `GET /api/v1/administradores` | Administrador | RF-55, RN-41 | `200` paginado; `search` (nombre, apellidos, cédula o correo) e `incluirInactivos` |
   | `GET /api/v1/administradores/{id}` | Administrador | RF-55 | `200` · `404` |
   | `PUT /api/v1/administradores/{id}` | Administrador | RF-55, RN-13 | `200` con el administrador · `404` · `409 RN-13` |
   | `POST /api/v1/usuarios/{id}/reenviar-temporal` | Administrador | RF-12, RN-23, RN-27, RN-29 | `200` con `{ correoEnviado }` · `409 RN-29` si la cuenta está inactiva |
   | `POST /api/v1/usuarios/{id}/desactivar` | Administrador | RF-14, RN-29, RN-30 | `204` · `409 RN-30` si es el último administrador activo |
   | `POST /api/v1/usuarios/{id}/reactivar` | Administrador | RF-14 | `204` |

   La respuesta de un administrador incluye `id`, `usuarioId`, `cedula`, `nombre`, `apellidos`, `telefono`, `correo`, `activo`, `fechaCreacion` y `ultimoIngreso`. Con `usuarioId` se llama a reenviar, desactivar y reactivar; los módulos `estudiantes` y `profesores` también lo expondrán.

   Detalle de los flujos:
   - **Login:** el correo se normaliza. Si la contraseña coincide con la definitiva, se anula la temporal pendiente (RF-11, RN-27). Si coincide con la temporal vigente, el token lleva `must_change_password`. Credenciales inválidas, correo inexistente o temporal vencida responden `401` con el mismo mensaje genérico. Con credenciales válidas y la cuenta inactiva, `403` con `code: "RN-29"`. Se registra `UltimoIngreso`.
   - **Cambio obligatorio:** `{ nuevaContrasena, confirmacion }`. La nueva cumple RN-26, coincide con la confirmación (`400`) y es distinta de la temporal y de la definitiva actual, si existe (`409 RN-25`). Guarda la definitiva, anula la temporal y regenera el stamp.
   - **Cambiar contraseña:** `{ contrasenaActual, nuevaContrasena, confirmacion }`. Contraseña actual incorrecta → `409` con `code: "RF-09"`. Mismas validaciones de la nueva.
   - **Aviso de cambio (RN-33):** tras el cambio obligatorio y tras cambiar la contraseña, se envía un correo de aviso.
   - **Olvido:** si el correo pertenece a una cuenta activa, se asigna una temporal de 1 hora que reemplaza a la anterior y se envía. La definitiva sigue funcionando (RN-32). Cuenta inexistente o inactiva: misma respuesta, sin correo.
   - **Registrar administrador:** `{ cedula, nombre, apellidos, telefono, correo }`. Crea el `Usuario` (rol `Administrador`, origen `Administrador`, `CreadoPorId` = el administrador actual, temporal de 72 horas) y el `Administrador` en un solo `SaveChangesAsync`, y después envía la temporal. Correo ya usado por cualquier cuenta → `409 RN-13`. Cédula de un administrador activo → `409 RN-13`; de uno inactivo → `409 RN-39`, con un mensaje que ofrece reactivarlo e incluye su `usuarioId`.
   - **Editar administrador:** `{ cedula, nombre, apellidos, telefono, correo }`, sobre cualquier administrador, incluido el propio. Si cambia el correo, se actualiza el `Usuario` (RN-31). Cédula o correo de otro registro → `409 RN-13`. Con el correo nuevo se puede reenviar la temporal (RF-12).
   - **Reenvío:** temporal de 72 horas con motivo `ReenvioAdministrador`; reemplaza a la anterior.
   - **Desactivar:** regenera el stamp, así los tokens vigentes dejan de funcionar. Un administrador puede desactivarse a sí mismo si no es el último activo. Reactivar no requiere nada más.

6. **Correos** (`IEmailSender` existente)
   - Plantillas HTML simples en español para: temporal de cuenta nueva, temporal por olvido, temporal reenviada y aviso de cambio de contraseña. Si la persona tiene datos personales en este módulo (administradores), el saludo usa su nombre.
   - Se envían **después** de un `SaveChangesAsync` exitoso. Si el envío falla, se registra en el log sin la contraseña, y la operación no se revierte: el administrador ve `correoEnviado: false` y puede reenviar. En el olvido y en el aviso, el fallo no cambia la respuesta.

7. **Generador de contraseñas temporales**: `RandomNumberGenerator`, 12 caracteres, siempre cumple RN-26. Singleton en Infrastructure.

8. **Rate limiting** con el middleware integrado de ASP.NET Core (sin paquetes nuevos), por IP: `login` 10 peticiones por minuto y `olvido-contrasena` 5 cada 15 minutos. Al superarlo, `429` como `ProblemDetails`.

9. **Seed del primer administrador** (`AdminSeedService`, ejecutado al inicio de `Program` antes de atender peticiones, y `AdminSeedOptions`)
   - Configuración: `Correo`, `Cedula`, `Nombre`, `Apellidos` y `Telefono` con valores genéricos y anónimos en `appsettings.json` (`admin@instituto.local`, `000000000`, `John`, `Doe`, `00000000`). `ContrasenaTemporal` solo en user-secrets o variables de entorno.
   - Si no existe ningún administrador, crea el `Usuario` y el `Administrador` con esos datos, motivo `CuentaNueva` y vencimiento de 72 horas. No envía correo. Los datos genéricos se corrigen después con `PUT /api/v1/administradores/{id}`.
   - Si el administrador inicial existe pero nunca definió su contraseña, en cada arranque renueva la temporal desde la configuración. Así un vencimiento no deja el sistema sin acceso.
   - Si no existe ningún administrador y falta `ContrasenaTemporal`, la API no arranca y lo indica con un mensaje claro.

10. **Servicio de cuentas para los módulos siguientes** (contrato en la frontera, decisiones 2 y 7 del mapa). También lo usa este módulo para registrar administradores.
    - Crear la cuenta de un registro público (rol `Estudiante`, contraseña definida por la persona, RN-19).
    - Crear una cuenta con temporal (rol indicado, `CreadoPorId` del administrador actual).
    - Cambiar el correo de una cuenta, validando RN-13 (para la edición de estudiantes, profesores y administradores).
    - Los métodos que crean o modifican agregan los cambios al contexto **sin** guardar, para que el módulo llamador los guarde junto con la persona en un solo `SaveChangesAsync`. Un método aparte envía la temporal después de guardar.
    - Todos validan el correo duplicado (`409 RN-13`).

11. **Verificaciones heredadas de `fundacion`**: el `409 RN-13` desde la BD y el `traceId` del `400`.

12. **Documentación**: README con las secciones `Jwt` y `AdminSeed` (qué va en user-secrets) y el paso de la migración; `Matricula.Api.http` con los flujos del módulo.

### Fuera de alcance

- Registro público y por administrador de estudiantes y profesores (RF-01 a RF-03): `estudiantes` y `profesores`, con el servicio del punto 10.
- Claims `estudiante_id` y `profesor_id`: los añaden `estudiantes` y `profesores` al login.
- Datos personales de estudiantes y profesores en `GET /api/v1/me`: los añaden sus módulos.
- Edición de los datos propios desde `/api/v1/me`: los administradores editan sus datos con `PUT /api/v1/administradores/{id}`.
- Guardas de RN-36 y RN-37 al desactivar: las añaden `matriculas` y `grupos` (decisión 4 del mapa), en el mismo método de desactivación de este módulo.
- Eliminación física de administradores: nunca, se desactivan (RN-35).
- Cambio de rol: no existe (decisión 3). Para que una persona asuma otro rol se registra una cuenta nueva en ese rol.
- Enlaces de un solo uso, refresh tokens, bloqueo de cuenta por intentos fallidos y auditoría (fuera del alcance de la versión).
- Pruebas de integración: diferidas.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, MailKit 4.18.1, xUnit 2.9.3. Ya referenciados y suficientes para este módulo: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12 (Api) y `Microsoft.Extensions.Identity.Core` 10.0.12 (Infrastructure, para `PasswordHasher<T>`). Rate limiting y `RandomNumberGenerator` vienen en el framework. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaUsuarioYAdministrador --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
smtp4dev --smtpport=2525 --urls=http://localhost:5080
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/Usuario.cs, Administrador.cs    nuevos; Usuario con sus reglas puras
  Enums/Rol.cs, MotivoTemporal.cs, OrigenRegistro.cs
  Reglas/PoliticaContrasena.cs             RN-26
src/Matricula.Application/
  Abstractions/IHasherContrasenas.cs, IGeneradorContrasenas.cs, IGeneradorToken.cs
  Abstractions/IAppDbContext.cs            + DbSet<Usuario>, DbSet<Administrador>
  Auth/                                    AuthService, DTOs, validadores, plantillas de correo
  Cuentas/                                 CuentaService (punto 10), UsuarioService (reenvío, activar/desactivar)
  Administradores/                         AdministradorService, DTOs, validadores
  DependencyInjection.cs                   + servicios
src/Matricula.Infrastructure/
  Persistence/Configurations/UsuarioConfiguration.cs, AdministradorConfiguration.cs
  Persistence/Migrations/                  AgregaUsuarioYAdministrador
  Persistence/AppDbContext.cs              + DbSet<Usuario>, DbSet<Administrador>
  Seguridad/HasherContrasenas.cs, GeneradorContrasenas.cs
  Seed/AdminSeedOptions.cs, AdminSeedService.cs
  DependencyInjection.cs                   + registros
src/Matricula.Api/
  Controllers/AuthController.cs, MeController.cs, AdministradoresController.cs, UsuariosController.cs
  Seguridad/JwtOptions.cs, GeneradorToken.cs, Politicas.cs, ValidacionSecurityStamp.cs
  Services/CurrentUser.cs                  + DebeCambiarContrasena
  Program.cs                               + autenticación, autorización, rate limiting, esquema Bearer
  appsettings*.json                        + Jwt (sin clave), AdminSeed (datos genéricos)
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/UsuarioTests.cs, PoliticaContrasenaTests.cs
README.md                                  + Jwt, AdminSeed
```

## Code Style

Igual que el código existente: constructores primarios, `ArgumentNullException.ThrowIfNull`, nombres de dominio en español y sufijos técnicos en inglés (en `Usuario` se conservan los nombres de §7 de la arquitectura: `PasswordHash`, `TempPasswordHash`…), un archivo por clase y pruebas `Metodo_Escenario_Resultado`. El dominio recibe hashes y fechas, nunca contraseñas en claro ni `DateTime.Now`.

```csharp
public void AsignarTemporal(string hash, MotivoTemporal motivo, DateTimeOffset ahora)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(hash);

    // RN-27: una nueva temporal reemplaza a la anterior.
    TempPasswordHash = hash;
    TempPasswordMotivo = motivo;
    TempPasswordExpira = ahora + (motivo == MotivoTemporal.Olvido ? TimeSpan.FromHours(1) : TimeSpan.FromHours(72));
}
```

```csharp
[Fact]
public void AsignarTemporal_PorOlvido_VenceEnUnaHora()
{
    var usuario = new Usuario();
    var ahora = new DateTimeOffset(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);

    usuario.AsignarTemporal("hash", MotivoTemporal.Olvido, ahora);

    Assert.Equal(ahora.AddHours(1), usuario.TempPasswordExpira);
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD):
  - `PoliticaContrasena` (RN-26): longitud mínima, mayúscula y número.
  - `Usuario`: vencimiento de 72 horas y de 1 hora (RN-23), reemplazo de la temporal (RN-27), vigencia antes y después del vencimiento, establecer la contraseña (anula la temporal, fija la fecha y cambia el stamp), ingreso con la definitiva (anula la temporal, RF-11), desactivar y reactivar (cambian el stamp), normalización del correo.
- **Manual**, con la API, PostgreSQL y smtp4dev en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Arranque sin administradores | Se crea el administrador inicial "John Doe" desde `AdminSeed` |
| Login del administrador inicial con la temporal | `200`, `debeCambiarContrasena: true` |
| Con ese token, `GET /api/v1/me` | `403`, `code: "RN-24"` |
| Cambio obligatorio con confirmación distinta | `400` con `traceId` (verificación heredada) |
| Cambio obligatorio con una nueva igual a la temporal | `409 RN-25` |
| Cambio obligatorio válido | `200` con token nuevo; aviso en smtp4dev; el token anterior responde `401` |
| `GET /api/v1/me` del administrador inicial | Datos de la cuenta y datos personales "John Doe" |
| Editar al administrador inicial con datos reales | `200`; `GET /api/v1/me` los muestra |
| Registrar un administrador | `201` + `Location`; temporal en smtp4dev con su nombre; la respuesta no contiene la contraseña |
| Registrar un administrador con un correo existente (con otra capitalización) | `409 RN-13` |
| Registrar un administrador con la cédula de otro activo / de uno inactivo | `409 RN-13` / `409 RN-39` con su `usuarioId` |
| `409 RN-13` desde la BD (verificación heredada) | Dos registros simultáneos con el mismo correo: uno `201` y el otro `409 RN-13` sin el nombre de la restricción |
| Editar el correo de un administrador y reenviarle la temporal | `200`; la temporal llega al correo nuevo; el login funciona con el correo nuevo |
| Listar administradores, con y sin `incluirInactivos` | Los inactivos solo aparecen con el filtro |
| Olvido con correo existente, inexistente e inactivo | Las tres respuestas son iguales; solo el existente y activo recibe correo |
| Login con la contraseña actual tras un olvido | `200` sin cambio pendiente; la temporal ya no sirve (RF-11) |
| Login con la temporal vencida | `401` |
| Reenviar temporal a una cuenta inactiva | `409 RN-29` |
| Desactivar una cuenta con sesión abierta | Su token responde `401`; su login responde `403 RN-29` |
| Desactivar al último administrador activo | `409 RN-30` |
| Logout | `204`; el token responde `401` |
| 11 logins en un minuto desde la misma IP | El último responde `429` |
| Endpoint de administrador con token de otro rol, o sin token | `403` / `401` como `ProblemDetails` |
| Logs | Ninguna contraseña ni temporal aparece en la consola |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Mensajes de error en español y comprensibles. Normalizar el correo antes de buscar o guardar. Guardar la cuenta y la persona en un solo `SaveChangesAsync` y enviar los correos después. `TimeProvider` para vencimientos e ingresos.
- **Preguntar antes:** añadir cualquier paquete NuGet. Cambiar el esquema de `usuario` o `administrador` fuera de esta spec. Cambiar la vigencia del token o los límites del rate limiting una vez aprobados.
- **Nunca:** devolver, mostrar o registrar en logs contraseñas, temporales o hashes. Poner la clave JWT o la temporal del seed en `appsettings*.json`. Eliminar usuarios o administradores físicamente (RN-35). Duplicar el correo en `Administrador`. `EnsureCreated` o `HasData` para el seed.

## Success Criteria

1. La migración `AgregaUsuarioYAdministrador` crea las tablas `usuario` (índice único en `correo`, FK `creado_por_id`) y `administrador` (índices únicos en `cedula` y `usuario_id`), con todas las FK sin cascada.
2. Los flujos de acceso de §3.2 de los requerimientos funcionan de punta a punta: login con definitiva y con temporal, cambio obligatorio, olvido, reenvío, cambio propio y logout.
3. Los administradores se registran, listan, consultan y editan con sus datos personales (RF-04, RF-55), y el inicial arranca con datos genéricos editables.
4. Ningún endpoint, salvo los anónimos de la spec, responde sin token; con cambio pendiente solo responden el cambio obligatorio y el logout.
5. Logout, desactivación y cambio de contraseña invalidan los tokens anteriores.
6. Las verificaciones manuales de la tabla, incluidas las heredadas de `fundacion`, dan el resultado esperado.
7. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Perfil en `GET /api/v1/me`**, no en `/api/v1/auth/perfil`: `estudiantes` ya usará `/api/me` y extenderá esa misma respuesta.
2. **La temporal sigue sirviendo hasta que se complete el cambio o venza.** Iniciar sesión con ella y salir sin cambiarla no la anula, para no dejar sin acceso a quien cierra el navegador. "Usarse" (RN-27) = completar el cambio obligatorio.
3. **El logout cierra todas las sesiones del usuario**, porque regenera el stamp (§7 de la arquitectura).
4. **Login con cuenta inactiva:** si la contraseña es correcta se informa `403 RN-29` ("contacte a la administración"); si no, el `401` genérico. No revela qué correos existen.
5. **Contraseña actual incorrecta en RF-09:** `409` con `code: "RF-09"`.
6. **Formato de los datos personales** (los requerimientos no lo fijan; `estudiantes` y `profesores` reutilizarán los mismos validadores):
   - Cédula: obligatoria, de 1 a 20 caracteres, solo letras, dígitos y guiones (admite cédulas nacionales, de residencia y pasaportes).
   - Nombre y apellidos: obligatorios, hasta 100 caracteres.
   - Teléfono: obligatorio, de 8 a 20 caracteres, solo dígitos, espacios, `+` y `-`.
7. **Cambiar el correo no cierra la sesión** del administrador editado: el correo no participa en la autorización.
8. **Límites de rate limiting** de 10 por minuto (login) y 5 cada 15 minutos (olvido), por IP.
9. **Seed:** renueva la temporal mientras el administrador inicial no haya definido su contraseña.
10. **Las pruebas unitarias cubren solo Domain**; el generador de temporales se verifica a mano (o con una prueba si se aprueba referenciar Infrastructure desde `Matricula.UnitTests`).
11. **Rutas inexistentes:** la política de respaldo también se aplica a las peticiones sin ruta, así que sin token responden `401` (no revelan qué rutas existen) y con sesión `404`. Con cambio pendiente, `403 RN-24`.

## Decisiones

1. **Datos personales de los administradores** y **correo solo en `Usuario`**: decisiones 6 y 7 del mapa (2026-10-19).
2. **Dos endpoints para cambiar la contraseña** (2026-10-19): `cambio-obligatorio` (RF-07, política `PasswordChangePending`) y `cambiar-contrasena` (RF-09, política por defecto), con DTOs y validadores distintos. `docs/arquitectura.md` §3 y §7 actualizados.
3. **El rol de una cuenta no cambia** (2026-10-19): decisión 8 del mapa. RN-20, RN-30 y RN-31 de los requerimientos y §7 de la arquitectura actualizados. RN-30 se aplica solo a la desactivación.

## Preguntas abiertas

Ninguna.
