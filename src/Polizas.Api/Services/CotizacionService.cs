using System.Net.Http.Json;

namespace Polizas.Api.Services;

public sealed record CotizacionDolar(
    string Moneda,
    string Casa,
    string Nombre,
    decimal Compra,
    decimal Venta,
    DateTimeOffset FechaActualizacion);

public interface ICotizacionService
{
    /// <summary>Devuelve null si el servicio externo no responde.</summary>
    Task<CotizacionDolar?> ObtenerDolarOficialAsync(CancellationToken ct);
}

/// <summary>
/// APRENDIZAJE (HttpClientFactory, curso sección 24): NUNCA hacer "new HttpClient()" en cada llamada.
/// El HttpClient llega inyectado ya configurado (BaseAddress, timeout) y su conexión subyacente
/// la administra la fábrica (reutiliza sockets y renueva el DNS). Esto es un "typed client".
/// Cuando una dependencia externa falla, la API propia tiene que degradar con elegancia (503), no explotar con un 500.
/// </summary>
public sealed class CotizacionService(HttpClient http, ILogger<CotizacionService> logger) : ICotizacionService
{
    public async Task<CotizacionDolar?> ObtenerDolarOficialAsync(CancellationToken ct)
    {
        try
        {
            return await http.GetFromJsonAsync<CotizacionDolar>("v1/dolares/oficial", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "No se pudo obtener la cotización del dólar");
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Si el token del request NO fue cancelado, entonces fue el timeout del HttpClient.
            logger.LogWarning(ex, "Timeout al obtener la cotización del dólar");
            return null;
        }
    }
}
