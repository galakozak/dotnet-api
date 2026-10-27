using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polizas.Api.Configuracion;
using Polizas.Api.Data;
using Polizas.Api.Demo;
using Polizas.Api.Endpoints;
using Polizas.Api.Infrastructure;
using Polizas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// 1) REGISTRO DE SERVICIOS (el "contenedor de inyección de dependencias")
//    Todo lo que se registra acá puede pedirse en el constructor de cualquier clase
//    o como parámetro de un endpoint.
// ═══════════════════════════════════════════════════════════════════════════

// JSON: los enums viajan como texto ("Vigente") en vez de números.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Errores: formato estándar ProblemDetails + un handler global para excepciones.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Validación automática de los DTOs de entrada en Minimal APIs (novedad de .NET 10).
builder.Services.AddValidation();

// Documentación OpenAPI nativa (desde .NET 9). El documento se publica en /openapi/v1.json.
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

// Reloj inyectable: en producción es el del sistema, en los tests se reemplaza por uno fijo.
builder.Services.AddSingleton(TimeProvider.System);

// Base de datos: DbContext con tiempo de vida Scoped (uno por request).
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Servicios de la aplicación. Se registra la interfaz -> implementación.
builder.Services.AddScoped<IPolizaService, PolizaService>();

// Configuración tipada + validada al arrancar (patrón Options).
builder.Services.AddOptions<CotizacionOptions>()
    .Bind(builder.Configuration.GetSection(CotizacionOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// HttpClientFactory: "typed client" configurado a partir de las Options.
builder.Services.AddHttpClient<ICotizacionService, CotizacionService>((sp, client) =>
{
    var opciones = sp.GetRequiredService<IOptions<CotizacionOptions>>().Value;
    client.BaseAddress = new Uri(opciones.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
});

// Demo de tiempos de vida (ver /Demo/CicloDeVida.cs).
builder.Services.AddSingleton<ISingletonId, OperacionId>();
builder.Services.AddScoped<IScopedId, OperacionId>();
builder.Services.AddTransient<ITransientId, OperacionId>();
builder.Services.AddScoped<ConsumidorDemo>();

// ═══════════════════════════════════════════════════════════════════════════
// 2) PIPELINE DE MIDDLEWARE: el ORDEN IMPORTA.
//    Cada request pasa por estas capas de arriba hacia abajo; la respuesta vuelve de abajo hacia arriba.
// ═══════════════════════════════════════════════════════════════════════════
var app = builder.Build();

app.UseMiddleware<TiempoDeRespuestaMiddleware>(); // el más externo: mide todo
app.UseExceptionHandler();                        // atrapa excepciones y llama al GlobalExceptionHandler
app.UseStatusCodePages();                         // 404/405 sin body pasan a tener ProblemDetails

// Crea la base si no existe. Para un proyecto real se usan migraciones (dotnet ef migrations add ...).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (app.Environment.IsDevelopment())
        DbInitializer.Seed(db);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // GET /openapi/v1.json

// ═══════════════════════════════════════════════════════════════════════════
// 3) ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════
app.MapHealthChecks("/health");
app.MapPolizasEndpoints();
app.MapOtrosEndpoints();

app.Run();

// Necesario para que el proyecto de tests pueda arrancar la API en memoria (WebApplicationFactory<Program>).
public partial class Program;
