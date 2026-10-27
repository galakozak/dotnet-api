using System.Diagnostics;

namespace Polizas.Api.Infrastructure;

/// <summary>
/// APRENDIZAJE (pipeline de middleware): cada request atraviesa los middlewares en el orden en que se
/// registran en Program.cs y la respuesta vuelve en orden inverso (como capas de cebolla).
/// Este middleware mide cuánto tarda el request, lo loguea y agrega el header X-Tiempo-Ms a la respuesta.
/// Está registrado primero (el más externo) para medir todo, incluso el manejo de errores.
/// </summary>
public sealed class TiempoDeRespuestaMiddleware(RequestDelegate siguiente, ILogger<TiempoDeRespuestaMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        var cronometro = Stopwatch.StartNew();

        // Los headers hay que escribirlos antes de que empiece el body: por eso OnStarting.
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers["X-Tiempo-Ms"] = cronometro.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await siguiente(contexto);

        logger.LogInformation(
            "{Metodo} {Ruta} -> {Status} en {Ms} ms",
            contexto.Request.Method,
            contexto.Request.Path,
            contexto.Response.StatusCode,
            cronometro.ElapsedMilliseconds);
    }
}
