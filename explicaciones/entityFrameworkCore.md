# Entity Framework Core: cómo funciona el `DbContext`

`DbContext` es la pieza central de Entity Framework Core (EF Core). A continuación, veremos qué hace utilizando tu propio código: `AppDbContext`, `IAppDbContext`, `DependencyInjection` y `AdministradorService`.

---

## 1\. La idea general

EF Core es un **ORM (Object-Relational Mapper)**. Permite trabajar con clases de C#, como `Usuario` y `Administrador`, mientras se encarga de traducir las operaciones a SQL para PostgreSQL.

El `DbContext` es el objeto que hace de puente entre el código C# y la base de datos.

Se encarga principalmente de cinco cosas:

1. **El modelo:** conoce qué clases corresponden a qué tablas y columnas.
2. **La conexión:** administra la conexión a la base de datos.
3. **Las consultas:** traduce las expresiones LINQ (`Where`, `Select`, etc.) a SQL.
4. **El seguimiento de cambios (****`Change Tracker`****):** registra qué objetos se han cargado y qué modificaciones han sufrido.
5. **El guardado:** mediante `SaveChangesAsync()`, persiste los cambios en la base de datos, normalmente dentro de una transacción.

---

## 2\. Tu `AppDbContext`, parte por parte

Archivo: `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`

```
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    // ...
}
```

Esta declaración significa lo siguiente:

- **Hereda de****`DbContext`****:** obtiene toda la funcionalidad de EF Core.
- **Recibe****`DbContextOptions<AppDbContext>`****:** contiene la configuración del contexto, como el proveedor de base de datos y otras opciones.
- **Implementa****`IAppDbContext`****:** expone las operaciones que necesita la capa de aplicación mediante una interfaz.

### 2.1. Los `DbSet`: las tablas

```
public DbSet<Usuario> Usuarios => Set<Usuario>();

public DbSet<Administrador> Administradores
    => Set<Administrador>();
```

Un `DbSet<T>` representa una colección de entidades de un tipo determinado y permite consultar, agregar o eliminar registros.

Por ejemplo:

```
db.Administradores.Where(a => a.Id == id);
```

Esta instrucción todavía no ejecuta la consulta. Construye una expresión LINQ que EF Core puede traducir a SQL.

La consulta se ejecuta cuando se utiliza un método que necesita obtener los resultados, como:

- `ToListAsync()`
- `SingleOrDefaultAsync()`
- `AnyAsync()`

### 2.2. `OnModelCreating`: cómo se mapean las clases a tablas

En tu contexto tienes esta instrucción:

```
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(AppDbContext).Assembly);
```

Este método busca y aplica las configuraciones de entidades que implementan `IEntityTypeConfiguration<T>` dentro del ensamblado indicado.

Por ejemplo, en `UsuarioConfiguration` podrías tener:

```
builder.ToTable("usuario");

builder.Property(u => u.Correo)
    .HasMaxLength(256);

builder.HasIndex(u => u.Correo)
    .IsUnique();

builder.HasOne<Usuario>()
    .WithMany()
    .HasForeignKey(u => u.CreadoPorId);
```

Cada instrucción define una regla de mapeo:

- `ToTable("usuario")`: asigna la entidad a la tabla `usuario`.
- `HasMaxLength(256)`: establece una longitud máxima de 256 caracteres para la propiedad.
- `HasIndex(...).IsUnique()`: configura un índice único para el correo.
- `HasForeignKey(...)`: define una clave foránea.

A continuación, tienes esta regla global:

```
foreach (var fk in modelBuilder.Model
    .GetEntityTypes()
    .SelectMany(e => e.GetForeignKeys()))
{
    fk.DeleteBehavior = DeleteBehavior.Restrict;
}
```

Recorre las claves foráneas del modelo y establece su comportamiento de eliminación en `Restrict`.

En términos prácticos, se evita eliminar un registro padre cuando existen registros relacionados que impiden esa operación. La base de datos rechazará el borrado si se incumple la restricción.

### 2.3. `ConfigureConventions`: reglas generales

```
configurationBuilder.Properties<Enum>()
    .HaveConversion<string>()
    .HaveMaxLength(30);

configurationBuilder.Properties<decimal>()
    .HavePrecision(10, 2);
```

Estas instrucciones establecen convenciones para las propiedades de determinados tipos.

- **Enumeraciones (****`Enum`****):** se almacenan como texto, por ejemplo, `"Administrador"`, en lugar de utilizar su valor numérico.
- **Decimales (****`decimal`****):** se configura una precisión de 10 dígitos en total, con 2 posiciones decimales.

### ¿Cuándo se construye el modelo?

El modelo de EF Core —tablas, columnas, índices y claves foráneas— se construye y se almacena en caché para reutilizarlo.

Ese modelo también se utiliza durante las migraciones, por ejemplo:

```
dotnet ef migrations add NombreDeLaMigracion
```

EF Core compara el modelo actual con el modelo registrado en la migración anterior y genera los cambios necesarios para actualizar el esquema de la base de datos.

---

## 3\. Cómo se configura y se crea: inyección de dependencias

Archivo: `src/Matricula.Infrastructure/DependencyInjection.cs`

La configuración del contexto tiene una estructura similar a esta:

```
services.AddDbContext<AppDbContext>((sp, options) => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(
        sp.GetRequiredService<AuditoriaInterceptor>()));

services.AddScoped<IAppDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());
```

- `UseNpgsql(connectionString)`: configura PostgreSQL como proveedor.
- `UseSnakeCaseNamingConvention()`: convierte los nombres a `snake_case`; por ejemplo, `FechaCreacion` pasa a `fecha_creacion`.
- `AddInterceptors(...)`: registra el interceptor de auditoría para participar en las operaciones correspondientes de EF Core.
- `AddScoped<IAppDbContext>(...)`: permite que los servicios que solicitan la interfaz reciban la instancia de `AppDbContext` registrada en el contenedor.

### 3.1. ¿Qué significa `Scoped`?

`AddDbContext` registra el contexto con un ciclo de vida `Scoped` de forma predeterminada. En una aplicación web con ASP.NET Core, esto normalmente significa una instancia por petición HTTP.

- Cada petición crea o reutiliza su propio `AppDbContext` dentro de ese ámbito.
- Al finalizar la petición, el contenedor libera el contexto.
- Los servicios del mismo ámbito pueden compartir esa instancia.

Por ejemplo, un servicio puede crear un `Usuario` y otro puede agregar un `Administrador` relacionado. Si ambos trabajan con el mismo contexto y se guardan los cambios conjuntamente, EF Core puede persistir las operaciones dentro de la misma unidad de trabajo.

**Importante:** `DbContext` no es seguro para el acceso concurrente entre varios hilos. Debe utilizarse durante un periodo limitado y no registrarse como `Singleton`.

### 3.2. ¿Por qué existe `IAppDbContext`?

La interfaz responde a una decisión de arquitectura por capas.

La capa `Application` no debería depender directamente de `Infrastructure`. Por eso:

- **Application:** define `IAppDbContext` con las operaciones que necesita, como los `DbSet` y `SaveChangesAsync()`.
- **Infrastructure:** implementa esa interfaz mediante `AppDbContext`.
- **Inyección de dependencias:** proporciona la implementación concreta cuando un servicio solicita la interfaz.

Así, `AdministradorService` puede trabajar con `IAppDbContext` sin conocer directamente los detalles de infraestructura.

---

## 4\. El `Change Tracker`: el corazón del `DbContext`

Este es uno de los conceptos más importantes de EF Core.

Veamos el método `ActualizarAsync` de `AdministradorService`:

```
var administrador = await db.Administradores
    .Include(a => a.Usuario)
    .SingleOrDefaultAsync(a => a.Id == id, ct);

administrador.ActualizarDatos(request.Cedula, ...);

await db.SaveChangesAsync(ct);
```

El proceso es el siguiente:

1. **Consulta:** EF Core ejecuta un `SELECT` para obtener el administrador y su usuario relacionado.
2. **Seguimiento:** si la consulta utiliza el seguimiento predeterminado, EF Core comienza a rastrear las entidades obtenidas.
3. **Modificación:** `ActualizarDatos(...)` cambia las propiedades del objeto en memoria.
4. **Detección de cambios:** al guardar, EF Core detecta qué propiedades han cambiado.
5. **Persistencia:** EF Core genera las instrucciones SQL necesarias, como un `UPDATE`.

Observa que no es necesario llamar a `db.Update(...)` en este caso.

Como la entidad ya está siendo rastreada, EF Core puede detectar los cambios automáticamente. En condiciones normales, compara el estado actual con los valores originales y actualiza las columnas modificadas.

### 4.1. Estados de una entidad

Cada entidad rastreada puede encontrarse en uno de los siguientes estados:

| Estado | Significado | Operación habitual al guardar |
| --- | --- | --- |
| `Added` | Se agregó una entidad nueva. | `INSERT` |
| `Unchanged` | La entidad está cargada y no tiene cambios detectados. | Ninguna |
| `Modified` | La entidad tiene cambios detectados. | `UPDATE` |
| `Deleted` | La entidad está marcada para eliminarse. | `DELETE` |
| `Detached` | EF Core no está rastreando la entidad. | Ninguna por sí sola |

Por ejemplo, al ejecutar:

```
db.Administradores.Add(nuevoAdministrador);
```

La entidad pasa al estado `Added`.

Cuando se ejecuta `SaveChangesAsync()`, EF Core genera el `INSERT` correspondiente.

### 4.2. `AsNoTracking()`: consultas de solo lectura

En métodos como `ListarAsync` y `ObtenerAsync` utilizas expresiones similares a esta:

```
db.Administradores.AsNoTracking()
```

Con `AsNoTracking()` le indicas a EF Core que no necesita mantener el seguimiento de las entidades obtenidas por esa consulta.

Esto puede reducir el consumo de memoria y el trabajo del seguimiento cuando solo necesitas leer datos.

La regla práctica es:

- **Vas a modificar y guardar una entidad:** utiliza una consulta normal con seguimiento.
- **Solo vas a leer datos o devolver un DTO:** considera `AsNoTracking()`.
- **Proyectas directamente a un DTO con****`Select(...)`****:** normalmente no necesitas seguimiento de entidades porque el resultado es un objeto de proyección, no una entidad rastreada.

---

## 5\. `SaveChangesAsync()` y tu interceptor de auditoría

`SaveChangesAsync()` coordina la persistencia de los cambios pendientes.

De forma simplificada, el proceso incluye:

1. Detectar los cambios en las entidades rastreadas.
2. Ejecutar los interceptores correspondientes a la operación de guardado.
3. Ejecutar las instrucciones SQL necesarias.
4. Confirmar la transacción si todo termina correctamente o revertirla si se produce un error dentro de esa transacción.

En tu `AuditoriaInterceptor` tienes una lógica similar a esta:

```
foreach (var entry in eventData.Context
    .ChangeTracker.Entries<EntidadBase>())
{
    if (entry.State == EntityState.Added)
    {
        entry.Entity.FechaCreacion = ahora;
    }
    else if (entry.State == EntityState.Modified)
    {
        entry.Entity.FechaModificacion = ahora;
    }
}
```

Este código recorre las entidades rastreadas que heredan de `EntidadBase` y establece las fechas de auditoría según su estado:

- `Added`: asigna `FechaCreacion`.
- `Modified`: asigna `FechaModificacion`.

De esta manera, los servicios no tienen que establecer manualmente esas fechas en cada operación.

Es un buen ejemplo de cómo el `Change Tracker` sirve para algo más que guardar datos: también permite implementar lógica transversal basada en el estado de las entidades.

### 5.1. El `DbContext` como unidad de trabajo

Por eso suele decirse que `DbContext` implementa el patrón **Unit of Work (unidad de trabajo)**.

Durante una operación, acumula los cambios pendientes y permite confirmarlos conjuntamente mediante una llamada a:

```
await db.SaveChangesAsync(ct);
```

Si el guardado se realiza dentro de una transacción, las operaciones incluidas en ella se confirman o revierten conjuntamente.

---

## 6\. Resumen visual del ciclo de vida en una petición HTTP

El siguiente diagrama muestra cómo se relacionan la inyección de dependencias, el servicio, el seguimiento de cambios y el guardado.

```
Request HTTP
    |
    v
DI crea/resuelve AppDbContext (Scoped)
    |
    v
Controller
    |
    v
AdministradorService(IAppDbContext db)
    |
    +--> Consulta Administradores
    |       |
    |       +--> SELECT
    |       +--> Entidad rastreada: Unchanged
    |
    +--> Modifica propiedades
    |       |
    |       +--> Estado: Modified
    |
    +--> Agrega una entidad nueva
    |       |
    |       +--> Estado: Added
    |
    +--> SaveChangesAsync()
            |
            +--> AuditoriaInterceptor establece fechas
            |
            +--> INSERT / UPDATE / DELETE
            |
            +--> COMMIT si todo termina correctamente
    |
    v
Response HTTP
    |
    v
Se libera AppDbContext
```

Este flujo representa el caso habitual en el que las entidades se consultan con seguimiento y las operaciones se realizan utilizando el mismo contexto.

---

## 7\. Errores típicos que debes tener en cuenta

### 7.1. Olvidar `SaveChangesAsync()`

Agregar o modificar una entidad en memoria no significa que los cambios ya estén guardados en la base de datos.

```
db.Administradores.Add(nuevoAdministrador);

// Falta guardar los cambios.
```

Si no llamas a `SaveChangesAsync()`, los cambios pendientes no se persisten y se pierden cuando se descarta el contexto.

### 7.2. Olvidar `Include()`

Si necesitas acceder a una relación, como `administrador.Usuario`, debes asegurarte de cargarla.

```
var administrador = await db.Administradores
    .Include(a => a.Usuario)
    .SingleOrDefaultAsync(a => a.Id == id, ct);
```

Sin `Include()`, la navegación no se carga automáticamente en una configuración habitual sin carga diferida. Por tanto, `administrador.Usuario` podría no estar disponible.

### 7.3. Materializar los datos antes de filtrar

Evita cargar toda una tabla en memoria para después filtrar:

```
var administradores = await db.Administradores
    .ToListAsync(ct);

var resultado = administradores
    .Where(a => a.Activo)
    .ToList();
```

Es preferible filtrar antes de materializar:

```
var resultado = await db.Administradores
    .Where(a => a.Activo)
    .ToListAsync(ct);
```

En el segundo caso, EF Core puede traducir el filtro a SQL para que la base de datos devuelva solo los registros que cumplen la condición.

### 7.4. El problema N+1

Este problema aparece cuando se ejecuta una consulta inicial y, después, se realiza otra consulta por cada elemento de un bucle para obtener sus datos relacionados.

En lugar de hacer muchas consultas individuales, considera:

- `Include()` para cargar relaciones.
- `Select(...)` para proyectar directamente los datos que necesitas.
- Otras estrategias de consulta que permitan reducir los viajes a la base de datos.

La opción adecuada depende de los datos que necesitas y de la forma en que vas a utilizarlos.

---

## 8\. Cómo ver el SQL que genera EF Core

Para comprender qué sucede cuando escribes una consulta LINQ, puedes habilitar el registro de comandos SQL en el entorno de desarrollo.

En `appsettings.Development.json`, puedes configurar un nivel de log similar a este:

```
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Esto permite observar los comandos SQL que EF Core ejecuta, lo que resulta útil para comprobar cómo se traducen los filtros, las relaciones y las operaciones de guardado.

Ten en cuenta que los registros pueden contener información sensible según las consultas y la configuración utilizada. Evita habilitar el registro de valores sensibles en entornos donde no corresponda.

---

## Conclusión

El `DbContext` es mucho más que una conexión a la base de datos. Es el componente que reúne el modelo de entidades, la ejecución de consultas, el seguimiento de cambios y la persistencia.

En tu arquitectura, `AppDbContext` implementa `IAppDbContext`, la inyección de dependencias administra su ciclo de vida y `AdministradorService` lo utiliza para consultar y modificar entidades. Finalmente, `SaveChangesAsync()` coordina la persistencia, mientras que `AuditoriaInterceptor` automatiza parte de la lógica de auditoría.

Comprender cómo se relacionan estos componentes te permitirá escribir servicios más claros, evitar consultas innecesarias y aprovechar mejor Entity Framework Core.