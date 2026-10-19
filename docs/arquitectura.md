# Arquitectura y buenas prácticas — API Sistema de Matrícula

Este documento fija las pautas de arquitectura y las buenas prácticas para la API del sistema de matrícula (ver `requerimientos.md`). El objetivo es una arquitectura **simple, en capas y fácil de mantener**, sin sobreingeniería.

---

## 1. Stack recomendado
| Pieza | Elección | Por qué |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | Soporte largo y la versión estable actual |
| ORM | **EF Core 10** + migraciones | Para acelerar el desarrollo, automatizar operaciones comunes (CRUD) y eliminar la necesidad de escribir código repetitivo |
| BD | **PostgreSQL** (proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`) | Gratis y robusta; nombres en snake_case con `EFCore.NamingConventions` |
| API | **Controllers** (no Minimal APIs) | Más ordenado con ~15 recursos y fácil de leer |
| Docs API | `Microsoft.AspNetCore.OpenApi` (integrado) + **Scalar** como UI | Swashbuckle ya no viene en las plantillas |
| Validación | **FluentValidation** (solo para los DTO de entrada) | Mensajes claros por campo |
| Auth | **JWT Bearer** + `PasswordHasher<T>` de Identity, **sin** ASP.NET Identity completo | El flujo de contraseñas temporales (RN-22 a RN-33) es propio y no encaja bien con Identity |
| Correo | Interfaz `IEmailSender` + **MailKit**; en desarrollo **smtp4dev** instalado como dotnet tool | Se puede cambiar de proveedor sin tocar la lógica |
| Pruebas | **Por ahora:** xUnit solo para pruebas **unitarias** de las reglas puras del dominio, sin BD. Los servicios y endpoints se verifican manualmente con Scalar, `Matricula.Api.http` y smtp4dev. **Diferido:** pruebas de integración con `WebApplicationFactory` sobre una BD **PostgreSQL local de pruebas** (`matricula_tests`), recreada con las migraciones en cada ejecución | Unitarias: rápidas y sin configuración. Cuando se retome la integración, **no** uses InMemory ni SQLite: no validan índices filtrados, FK, transacciones ni `xmin` |

**Lo que NO usaría (para mantenerlo simple):** MediatR/CQRS, AutoMapper, un repositorio genérico encima de EF, Clean Architecture completa ni DDD con agregados estrictos. `DbContext` ya cumple el papel de repositorio y de Unit of Work.

## 2. Arquitectura en capas

### 2.1 Resumen

| Capa | Propósito | Lógica de negocio | Acceso a la BD | Elementos |
|---|---|:---:|:---:|---|
| **Domain** | Modelo del negocio | ✔ (reglas sobre el propio objeto) | ✘ | Entidades (clases modelo), enums, excepciones de dominio |
| **Application** | Casos de uso | ✔ (la mayor parte) | ✔ vía `IAppDbContext` (LINQ) | Servicios, DTOs, validadores, interfaces |
| **Infrastructure** | Implementaciones técnicas | ✘ | ✔ (la implementa) | `AppDbContext`, configuraciones EF, migraciones, correo, seed |
| **Api** | Exposición HTTP | ✘ | ✘ | Controllers, `Program.cs`, middleware, filtros, auth |

**No hay capa de repositorios.** `DbContext` ya funciona como repositorio (`DbSet<T>`) y como Unit of Work (`SaveChangesAsync`). Los servicios lo usan directamente a través de la interfaz `IAppDbContext`.

### 2.2 Detalle por capa

**Matricula.Domain**: no depende de nada, ni siquiera de EF Core.
- `Entities/`: las clases modelo, mapeadas a tablas: `Estudiante`, `Profesor`, `Administrador`, `Curso`, `Periodo`, `Aula`, `Grupo`, `Horario`, `Matricula`, `Usuario`. Heredan de `EntidadBase`.
- `Enums/`: `EstadoGrupo`, `Modalidad`, `EstadoMatricula`, `Rol`, `NivelCurso`…
- `Exceptions/`: `BusinessRuleException(code, mensaje)`.
- Métodos con reglas que solo dependen del propio objeto, por ejemplo:
  - `Grupo.CambiarEstado()` (transiciones de RF-47)
  - `Horario.SeTraslapaCon()` (RN-14)
  - `Periodo.EnRangoMatricula()`
  - `Matricula.Retirar()`

**Matricula.Application**: aquí vive la lógica de negocio. Depende de Domain y del paquete EF Core, este último solo para usar LINQ asíncrono.
- `Abstractions/`: las interfaces que implementa Infrastructure: `IAppDbContext`, `IEmailSender`, `ICurrentUser`.
- `<Funcionalidad>/` (por ejemplo, `Matriculas/`), con:
  - `MatriculaService`: un método por caso de uso.
    - **Consulta y guarda** a través de `IAppDbContext`.
    - **Valida las reglas que necesitan datos** de la BD: cupo, traslapes, duplicados, permisos sobre el recurso.
    - Invoca los métodos del dominio.
    - Envía correos.
  - DTOs: `MatricularRequest`, `MatriculaResponse`…
  - Validadores de FluentValidation para el formato de los DTOs de entrada.
- `Common/`: excepciones de aplicación (`NotFoundException`, `ConflictException`, `ForbiddenException`) y paginación.
- `DependencyInjection.cs`: `AddApplication()` registra los servicios y validadores.

**Matricula.Infrastructure**: los detalles técnicos. Depende de Application y de Domain.
- `Persistence/AppDbContext.cs`: el `DbContext` de EF Core. Implementa `IAppDbContext`.
- `Persistence/Configurations/`: un `IEntityTypeConfiguration<T>` por entidad, con el mapeo (tablas, longitudes, índices únicos, relaciones, token de concurrencia `xmin`).
- `Persistence/Migrations/` e `Interceptors/` (auditoría de fechas).
- `Email/`: `SmtpEmailSender` (MailKit), que implementa `IEmailSender`.
- Generador de contraseñas temporales y seed del administrador inicial.
- `DependencyInjection.cs`: `AddInfrastructure()` registra el `DbContext`, el correo y las opciones.

**Matricula.Api**: entrada HTTP. Depende de Application, y de Infrastructure solo para el registro en `Program.cs`.
- `Controllers/`: reciben el DTO, llaman al servicio y devuelven el resultado HTTP. **No** tienen lógica ni usan el `DbContext`.
- `Common/`: `ApiControllerBase`, `GlobalExceptionHandler` (excepción → `ProblemDetails` con status 404/409/403…) y `ValidationFilter`.
- `Services/CurrentUser.cs`: implementa `ICurrentUser` leyendo los claims del JWT.
- `Program.cs`: inyección de dependencias, pipeline, autenticación y autorización, OpenAPI/Scalar.

### 2.3 Dependencias entre proyectos
```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure
```
Infrastructure implementa las interfaces definidas en Application (inversión de dependencias). Así la lógica de negocio no depende de la implementación concreta de la BD ni del correo.

### 2.4 Flujo de una petición (ej. matricular)
1. `MatriculasController` recibe el `POST`. `ValidationFilter` valida el formato del DTO y, si falla, responde `400`.
2. `MatriculaService.MatricularAsync()` carga el estudiante y el grupo con `IAppDbContext`, valida RN-01 a RN-06 y crea la `Matricula`.
3. `SaveChangesAsync()` persiste los cambios a través de `AppDbContext` en PostgreSQL.
4. Si una regla falla, se lanza una `BusinessRuleException`, que `GlobalExceptionHandler` convierte en un `409` con `"code": "RN-xx"`.

### 2.5 Convenciones
- Organizar **por funcionalidad** dentro de Application: `Estudiantes/`, `Grupos/`, `Matriculas/`, `Auth/`…
- Un servicio por funcionalidad: `EstudianteService`, `GrupoService`, `MatriculaService`, `AuthService`, `PeriodoService`…
- Las entidades nunca salen de Application: los controllers reciben y devuelven DTOs.

## 3. Diseño de la API (REST)
- **Recursos en plural**, en minúscula y con kebab-case: `/api/estudiantes`, `/api/profesores`, `/api/cursos`, `/api/periodos`, `/api/aulas`, `/api/grupos`, `/api/matriculas`.
- **Subrecursos para las relaciones:** `GET /api/grupos/{id}/estudiantes`, `GET /api/estudiantes/{id}/matriculas?periodoId=`, `GET /api/profesores/{id}/horario`, `POST /api/grupos/{id}/horarios`, `DELETE /api/grupos/{id}/horarios/{horarioId}`.
- **Acciones de negocio** que no son un CRUD se modelan como sub-recurso con POST:
  - `POST /api/grupos/{id}/estado` con body `{ "estado": "Abierto" }`
  - `POST /api/estudiantes/{id}/desactivar` y `.../reactivar`
  - `POST /api/matriculas` (matricular), `POST /api/matriculas/{id}/retiro`, `POST /api/matriculas/{id}/cambio-grupo`
  - `POST /api/auth/login`, `/api/auth/olvido-contrasena`, `/api/auth/cambio-obligatorio` (RF-07), `/api/auth/cambiar-contrasena` (RF-09), `/api/auth/logout`, `GET /api/auth/perfil` (o `/api/me`)
  - `POST /api/usuarios/{id}/reenviar-temporal`
- **"Lo mío":** usa `/api/me/matriculas`, `/api/me/horario` y `/api/me/grupos`, en lugar de que el cliente envíe su propio id.
- **Códigos HTTP coherentes:** `200` · `201` + `Location` al crear · `204` sin contenido · `400` validación · `401` sin sesión · `403` sin permiso o con cambio de contraseña pendiente · `404` · `409` choque con una regla de negocio, un dato duplicado o concurrencia.
- **Errores con `ProblemDetails` (RFC 9457)** en todas las respuestas, con una extensión `code` que referencie la regla, por ejemplo `"code": "RN-05"`, más un mensaje legible. Esto cubre el requisito de "mensajes claros".
- **Paginación estándar:** `?page=1&pageSize=20&search=...&incluirInactivos=false&orden=nombre`. La respuesta es `{ items, page, pageSize, totalCount }`. Pon un `pageSize` máximo (por ejemplo, 100).
- **DTOs separados** para entrada y salida (`CrearGrupoRequest`, `GrupoResponse`, `GrupoDetalleResponse`). **Nunca expongas las entidades.** Haz el mapeo a mano o con métodos de extensión.
- **Tipos:** `DateOnly` para fechas, `TimeOnly` para las horas del horario, `DateTimeOffset`/UTC para marcas de tiempo. Serializa los enums como string (`JsonStringEnumConverter`).
- Versionado: basta con el prefijo `/api/v1` desde el inicio; no hace falta una librería de versionado.

### 3.1 Documentación de la API: OpenAPI y Scalar
Son dos piezas distintas que trabajan juntas.

| Pieza | Qué es | Dónde se ve |
|---|---|---|
| **OpenAPI** | Especificación estándar que describe la API en JSON: endpoints, parámetros, DTOs de entrada y salida con sus tipos, códigos de respuesta y autenticación. La genera automáticamente el paquete `Microsoft.AspNetCore.OpenApi` a partir de los controllers y DTOs. Es un contrato pensado para máquinas. | `http://localhost:5022/openapi/v1.json` |
| **Scalar** | Interfaz web que lee el JSON de OpenAPI y lo muestra de forma navegable. Permite **probar los endpoints desde el navegador** (incluido el token JWT) y ver ejemplos de llamada en curl, JavaScript, C#, etc. Cumple el mismo papel que Swagger UI. | `http://localhost:5022/scalar` |

**Por qué Scalar y no Swagger:** desde .NET 9, Microsoft dejó de incluir Swashbuckle en las plantillas y genera el OpenAPI con su propio paquete. Scalar es la interfaz más usada con ese paquete. Swagger UI también funcionaría con el mismo JSON.

**Para qué sirve:**
- **Documentación siempre actualizada:** se genera del código, así que no puede quedar desfasada.
- **Probar durante el desarrollo** sin necesitar Postman.
- **Integración con el frontend:** a partir de `openapi/v1.json` se pueden generar los tipos o clientes TypeScript (por ejemplo, con `openapi-typescript` o NSwag).

**Pautas:**
- Ambos se habilitan **solo en Development** (`Program.cs`), para no exponer la estructura de la API en producción.
- Para que la documentación sea útil:
  - Declara los tipos de respuesta en los controllers (`[ProducesResponseType]` o `ActionResult<T>`).
  - Usa DTOs con nombres claros.
  - Agrega comentarios XML `///` en los endpoints que lo ameriten.

## 4. Buenas prácticas de .NET
- `<Nullable>enable</Nullable>`, `ImplicitUsings`, y advertencias como errores en un `Directory.Build.props` común. Añade un `.editorconfig`.
- **Async en todo** y propaga `CancellationToken` desde el controller hasta EF.
- **Options pattern** para la configuración tipada: `JwtOptions`, `SmtpOptions`, `AdminSeedOptions`, `PasswordPolicyOptions`. Los secretos van en **user-secrets** o en variables de entorno, nunca en `appsettings.json`.
- **Manejo global de errores** con `IExceptionHandler` + `AddProblemDetails()`. Excepciones propias y pocas: `NotFoundException`, `BusinessRuleException(code, mensaje)`, `ConflictException` y `ForbiddenException`. Cada una se mapea a un status. *(Alternativa: un `Result<T>`. Las excepciones son más simples para este tamaño.)*
- **Validación en dos niveles:**
  - FluentValidation revisa el formato del DTO: obligatorios, longitudes, política de contraseña RN-26, `HoraFin > HoraInicio` RN-15 y fechas RN-16.
  - El **servicio** revisa las reglas que necesitan la BD: cupo, traslapes y duplicados.
- **Tiempo testeable:** inyecta `TimeProvider` en lugar de usar `DateTime.Now`. Lo necesitan RN-04, RN-10 y los vencimientos de las temporales.
- Logging con `ILogger<T>` y mensajes estructurados. Serilog es opcional. **Nunca registres contraseñas ni temporales.**
- Ciclos de vida en la inyección de dependencias: el `DbContext` y los servicios son `Scoped`; `TimeProvider` y el generador de contraseñas, `Singleton`.
- Health check (`/health`), CORS configurado para el front y **rate limiting** integrado en `login` y `olvido-contrasena`.

## 5. Buenas prácticas de EF Core
- **Fluent API** en una clase `IEntityTypeConfiguration<T>` por entidad, cargadas con `ApplyConfigurationsFromAssembly`. No uses data annotations en el dominio.
- **Solo migraciones.** Nunca uses `EnsureCreated`. En producción, aplica las migraciones con un script o bundle.
- **Sin cascadas** (sección 4.2): configura `DeleteBehavior.Restrict` de forma global en `OnModelCreating` para todas las FK.
- **Índices únicos** para RN-13: cédula, correo, código de curso, NRC, código de aula y nombre de periodo. Captura la violación de unicidad y devuelve `409`, pero valida antes en el servicio para dar un mensaje bonito.
- Guarda los **enums como string** (`HasConversion<string>()`) para que la BD sea legible.
- Fija la longitud máxima de los strings y la precisión de `Costo` (`decimal(10,2)`).
- **Lecturas:** usa `AsNoTracking()` y **proyecta a DTO con `Select`**. No cargues entidades completas para listar. No actives lazy loading, y usa `Include` solo cuando vayas a modificar.
- **Paginación** con `OrderBy` + `Skip/Take` y un `CountAsync` aparte.
- **No uses un filtro global para "Activo".** RN-40 exige que el historial muestre registros inactivos, así que filtra explícitamente en los listados (RN-41).
- **Atomicidad:** un solo `SaveChangesAsync` ya es transaccional. Con eso alcanza para el cambio de grupo (RF-53) y para cancelar un grupo y retirar sus matrículas (RN-12). Usa `BeginTransactionAsync` solo si necesitas varios `SaveChanges`.
- **Concurrencia del último cupo:** usa la columna de sistema `xmin` de PostgreSQL como token de concurrencia en `Grupo` (propiedad `uint Version` con `.IsRowVersion()`; Npgsql la mapea a `xmin`) y un contador `CuposOcupados` que se actualiza al matricular o retirar. Si dos estudiantes compiten, uno recibe `DbUpdateConcurrencyException`, que se convierte en `409` con el mensaje "El grupo ya no tiene cupo, intenta de nuevo".
- **Unicidad de matrícula activa:** un índice único filtrado `(EstudianteId, GrupoId) WHERE estado = 'Activa'` (en EF: `.HasFilter("estado = 'Activa'")`; el filtro usa SQL de PostgreSQL y los nombres en snake_case). RN-05 (mismo curso y periodo) se valida en el servicio.
- **Auditoría ligera:** `FechaCreacion` y `FechaModificacion` asignadas en un `SaveChangesInterceptor`.
- **Seed del primer administrador** al arrancar (`IHostedService` o al inicio de `Program`), leyendo `AdminSeedOptions`. No uses `HasData`, porque la contraseña viene de la configuración.

## 6. Modelo de dominio (nivel de "inteligencia")
Entidades mayormente simples, con **un poco de comportamiento donde aporta**:
- `Grupo.CambiarEstado(nuevo)` valida las transiciones de RF-47 (Planificado → Abierto → Cerrado → En curso → Finalizado, y Cancelado desde cualquier estado salvo Finalizado).
- `Horario.SeTraslapaCon(otro)` implementa RN-14: mismo día y `a.Inicio < b.Fin && b.Inicio < a.Fin`.
- `Periodo` con métodos `EnRangoMatricula(fecha)` y `PermiteRetiro(fecha)`.
- `Matricula.Retirar(fecha, motivo)`.

Así las reglas puras se prueban con tests unitarios sin BD, y los servicios orquestan.

## 7. Seguridad (lo específico de este sistema)
- `Usuario` tiene: `PasswordHash?`, `TempPasswordHash?`, `TempPasswordExpira`, `TempPasswordMotivo`, `Rol`, `Activo` y `SecurityStamp`. Ambos hashes se calculan con `PasswordHasher<Usuario>`.
- **El rol es inmutable (RN-31):** `Usuario.Rol` se fija al crear la cuenta (propiedad `init`) y no existe ningún endpoint ni método de servicio para cambiarlo. Una persona con otro rol recibe otra cuenta, con otro correo, y su registro de `Estudiante`, `Profesor` o `Administrador` correspondiente; la misma cédula puede repetirse entre tablas. Así el rol del token, los permisos y el historial de cada cuenta nunca se desfasan.
- **Login:**
  - Si la contraseña coincide con la definitiva, limpia la temporal (RN-11/RN-27).
  - Si coincide con la temporal vigente, emite un JWT con el claim `must_change_password=true`.
- **RN-24:** una **política de autorización por defecto** exige que **no** exista ese claim. Solo `cambio-obligatorio` y `logout` permiten tenerlo (`[Authorize(Policy="PasswordChangePending")]`).
- **Roles:** `[Authorize(Roles = "Administrador")]` o políticas con nombre.
- **Propiedad (RN-18):** se verifica en el servicio usando `ICurrentUser` (Id, Rol, EstudianteId, ProfesorId leídos de los claims), no solo con atributos.
- **Logout, desactivación y cambio de contraseña con JWT:** usa tokens de vida corta (por ejemplo, 30–60 min) y un claim con el `SecurityStamp`. Al cambiar la contraseña, desactivar la cuenta o cerrar sesión, se regenera el stamp, y un evento `OnTokenValidated` lo compara contra la BD. Es simple y cumple RN-29.
- Contraseñas temporales con `RandomNumberGenerator` que cumplan RN-26. Los correos se envían **después** de un `SaveChanges` exitoso.
- RN-28: el endpoint de olvido de contraseña siempre responde `200` con el mismo mensaje.

## 8. Convenciones
- **Idioma:**
  - Nombres de **dominio en español y sin tildes ni ñ** (`Matricula`, `Estudiante`, `Periodo`).
  - Sufijos técnicos en inglés (`Service`, `Controller`, `Request`, `Response`, `Configuration`).
  - Elige la convención una vez y mantenla.
- Rutas en minúscula y JSON en camelCase (el valor por defecto).
- Un archivo por clase. Pocos comentarios: solo para explicar *por qué* se hace algo.
- Usa Git desde el día uno, con commits pequeños, y añade un `README` con los pasos para levantar el proyecto. En desarrollo, PostgreSQL y smtp4dev se instalan localmente; el `docker-compose.yml` queda como alternativa opcional (por ejemplo, para el despliegue futuro).

## 9. Orden sugerido de implementación
1. Esqueleto de la solución, `Directory.Build.props`, `.editorconfig`, BD local de desarrollo, smtp4dev. (La BD de pruebas `matricula_tests` se configura cuando se retomen las pruebas de integración.)
2. Dominio + `AppDbContext` + configuraciones + primera migración.
3. Infraestructura transversal: ProblemDetails, manejo de excepciones, paginación, OpenAPI/Scalar.
4. Auth completo (login, temporales, cambio obligatorio, olvido, seed del admin, correo).
5. Catálogos: cursos, periodos, aulas, profesores, estudiantes (CRUD + desactivar).
6. Grupos + horarios + validación de traslapes (RN-07/08/09).
7. Matrícula, retiro y cambio de grupo + concurrencia.
8. Consultas y listados de la sección 6.

