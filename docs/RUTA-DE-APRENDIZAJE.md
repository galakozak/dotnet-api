# Ruta de aprendizaje

Recomendación: **ver la sección del curso, abrir el archivo indicado, ejecutar el request correspondiente y hacer el ejercicio**.
Cada etapa tiene que terminar con `dotnet test` en verde antes de pasar a la siguiente.

---

## Etapa 1: Cómo arranca y cómo viaja un request

**Curso:** secciones 22 y 23 (API desde cero, Minimal APIs)
**Mirar:** `Program.cs` (leerlo de arriba a abajo, tiene tres bloques: servicios, pipeline, endpoints)

Conceptos:
- `WebApplication.CreateBuilder` → `Build()` → `Run()`.
- El pipeline de middleware: **el orden importa**. `TiempoDeRespuestaMiddleware` va primero para medir todo.

Ejercicios:
1. Ejecutar `GET /api/polizas/1` y mirar el header `X-Tiempo-Ms` en la respuesta.
2. Mover `app.UseExceptionHandler()` *antes* de `UseMiddleware<TiempoDeRespuestaMiddleware>()`. ¿Qué status loguea el middleware cuando hay un error? ¿Por qué?
3. Escribir un middleware propio que rechace con 400 los requests sin el header `X-Correlation-Id`.

## Etapa 2: REST y Minimal APIs

**Curso:** sección 19 (principios REST), sección 23
**Mirar:** `Endpoints/PolizasEndpoints.cs`

Conceptos:
- Sustantivos en plural en la URL, el verbo HTTP define la acción.
- Códigos de estado correctos: 200, 201 + `Location`, 204, 400, 404, 409.
- `TypedResults` y `Results<A, B>`: el tipo de retorno documenta las respuestas posibles.
- `MapGroup` para agrupar rutas, `[AsParameters]` para agrupar parámetros de query.

Ejercicios:
1. Agregar `GET /api/polizas/{id}/resumen` que devuelva solo número, tomador y estado.
2. Agregar un parámetro de orden (`?orden=prima`) al listado.
3. Explicar por qué anular es un `POST /{id}/anular` y no un `DELETE`.

## Etapa 3: DTOs, entidades y validación

**Curso:** sección 12 (excepciones), secciones 8 a 10 (clases, interfaces)
**Mirar:** `Domain/Poliza.cs`, `Dtos/GuardarPolizaRequest.cs`, `Dtos/PolizaDto.cs`

Conceptos:
- Entidad (persistencia) ≠ DTO (contrato de la API).
- Validación con atributos + `IValidatableObject` para reglas entre campos. En .NET 10 se activa con `AddValidation()`.
- Las reglas de negocio viven en el dominio (`Poliza.Anular`), no en los endpoints.

Ejercicios:
1. Ejecutar en `requests.http` el request de datos inválidos y leer el JSON de errores completo.
2. Agregar la regla "la vigencia no puede superar 2 años" y un test que la cubra.
3. Agregar el campo `Producto` (Auto, Hogar, Vida) como enum en entidad, DTOs y validación.

## Etapa 4: Inyección de dependencias

**Curso:** sección 20 (inyección de dependencias), sección 18 (SOLID)
**Mirar:** `Demo/CicloDeVida.cs`, `Services/IPolizaService.cs`, el bloque de registro en `Program.cs`

Conceptos:
- Registrar interfaz → implementación. Los endpoints dependen de la abstracción.
- Singleton / Scoped / Transient. El `DbContext` es Scoped.
- Por qué se inyecta `TimeProvider` en vez de usar `DateTime.UtcNow`.

Ejercicios:
1. Llamar varias veces a `GET /api/demo/ciclo-de-vida` y explicar qué Guid cambia y por qué.
2. Cambiar `IScopedId` a Singleton y ver qué test deja de pasar.
3. Crear `IGeneradorDeNumeroDePoliza` (inyectado en `PolizaService`) para generar el número automáticamente cuando no viene en el request.

## Etapa 5: Base de datos con EF Core

**Curso:** sección 17 (Entity Framework Core), sección 15 (LINQ), sección 14 (async)
**Mirar:** `Data/AppDbContext.cs`, `Services/PolizaService.cs`

Conceptos:
- `DbContext`, `DbSet`, configuración del modelo, índice único.
- Consultas LINQ de ejecución diferida: se arman y se ejecutan con `ToListAsync` / `CountAsync`.
- `AsNoTracking`, proyecciones con `Select`, paginación con `Skip/Take`.
- `async/await` y `CancellationToken` de punta a punta.
- Tracking de cambios: `ActualizarAsync` no llama a ningún `Update()`, solo modifica y hace `SaveChanges`.

Ejercicios:
1. Con `Microsoft.EntityFrameworkCore.Database.Command` en `Information` (ya está en `appsettings.Development.json`) mirar en consola el SQL que genera cada endpoint.
2. Agregar la entidad `Cobertura` (una póliza tiene muchas) con relación 1-a-N y un endpoint para agregarlas. Probar `Include()` y ver el SQL.
3. Reemplazar `EnsureCreated()` por migraciones (`dotnet ef migrations add Inicial`).

## Etapa 6: Errores y observabilidad

**Curso:** sección 12 (excepciones)
**Mirar:** `Infrastructure/GlobalExceptionHandler.cs`, el uso de `ILogger` en `PolizaService`

Conceptos:
- Un único handler convierte excepciones en `ProblemDetails`. Los endpoints no tienen `try/catch`.
- Errores de negocio (409, mensaje visible) vs. errores inesperados (500, sin detalle y con log completo).
- Logging estructurado: `{Numero}` es una propiedad, no texto pegado.
- Health checks.

Ejercicios:
1. Provocar un 500 a propósito (lanzar `InvalidOperationException` en un endpoint) y comparar lo que ve el cliente con lo que queda en el log.
2. Agregar `PolizaNoVigenteException` → 422 en el handler.
3. Agregar un `TraceId` al log de cada request.

## Etapa 7: Consumir otras APIs

**Curso:** sección 24 (HttpClient Factory)
**Mirar:** `Services/CotizacionService.cs`, `Configuracion/CotizacionOptions.cs`, el registro en `Program.cs`

Conceptos:
- `HttpClientFactory` y "typed clients": nunca `new HttpClient()`.
- Patrón Options: configuración tipada y validada al arrancar (`ValidateOnStart`).
- Timeouts y degradación elegante: si la dependencia falla, la API responde 503 y no 500.

Ejercicios:
1. Poner una `BaseUrl` inválida en `appsettings.json` y ver que la app ni siquiera arranca.
2. Agregar `GET /api/polizas/{id}/prima-en-dolares` usando la cotización.
3. Agregar reintentos con `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`).

## Etapa 8: Tests de integración

**Mirar:** `tests/Polizas.Api.Tests/`

Conceptos:
- `WebApplicationFactory<Program>`: la API real corriendo en memoria.
- Se reemplaza solo lo que sale del proceso (base, HTTP externo, reloj).
- Se prueba el contrato (status + body), no la implementación.

Ejercicios:
1. Agregar un test para `PUT` (éxito, 404 y 409 por número duplicado).
2. Agregar un test que verifique que una póliza anulada no se puede actualizar.
3. Romper a propósito una regla de negocio y comprobar que el test la detecta.

---

## Después de esto: temas que el proyecto todavía no cubre

| Tema | Por dónde empezar |
|---|---|
| Autenticación y autorización (JWT, roles, policies) | Paquete `Microsoft.AspNetCore.Authentication.JwtBearer`, `RequireAuthorization()` en el grupo de endpoints |
| UI de documentación | Agregar Scalar o Swagger UI sobre `/openapi/v1.json` |
| Rate limiting | `AddRateLimiter` + `RequireRateLimiting` (viene en el framework) |
| Caché | `HybridCache` (.NET 9) y output caching |
| Base real (SQL Server / PostgreSQL) | Cambiar el provider de EF Core y la cadena de conexión |
| Contenedores | `Dockerfile` multi-stage con `dotnet publish` |
| Versionado de API | `Asp.Versioning.Http` |
| Pipeline CI/CD | Ya hay un `ci.yml` básico en `.github/workflows`: extenderlo con publicación |

## Recursos

- Microsoft Learn: «What's new in ASP.NET Core 10» y «What's new in .NET 10» (también las páginas de .NET 8 y 9).
- Documentación de Minimal APIs, EF Core y OpenAPI en learn.microsoft.com.
