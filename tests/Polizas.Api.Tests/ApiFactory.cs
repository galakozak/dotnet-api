using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Polizas.Api.Services;

namespace Polizas.Api.Tests;

/// <summary>
/// APRENDIZAJE (tests de integración): WebApplicationFactory levanta la API REAL en memoria, con todo su pipeline
/// (routing, validación, manejo de errores, EF Core). Solo se reemplaza lo que tiene efectos externos:
///  - la base: un archivo SQLite temporal propio de esta fábrica,
///  - el servicio de cotización (que llamaría a internet),
///  - el reloj.
/// El resto del sistema se prueba tal cual corre en producción.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _rutaDb = Path.Combine(Path.GetTempPath(), $"polizas-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" (y no "Development") para que no se cargue la data de ejemplo.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_rutaDb}");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.AddSingleton<ICotizacionService, CotizacionFalsa>();
            servicios.AddSingleton<TimeProvider>(new RelojFijo(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;

        try { File.Delete(_rutaDb); }
        catch (IOException) { /* en Windows el archivo puede seguir tomado: no importa, es temporal */ }
    }
}

/// <summary>Doble de prueba: responde siempre lo mismo, sin salir a internet.</summary>
public sealed class CotizacionFalsa : ICotizacionService
{
    public Task<CotizacionDolar?> ObtenerDolarOficialAsync(CancellationToken ct) =>
        Task.FromResult<CotizacionDolar?>(new CotizacionDolar(
            "USD", "oficial", "Oficial", 1000m, 1050m, new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)));
}

public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
