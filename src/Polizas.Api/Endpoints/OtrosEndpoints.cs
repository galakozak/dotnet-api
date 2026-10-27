using Microsoft.AspNetCore.Http.HttpResults;
using Polizas.Api.Demo;
using Polizas.Api.Services;

namespace Polizas.Api.Endpoints;

public static class OtrosEndpoints
{
    public static IEndpointRouteBuilder MapOtrosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cotizacion/dolar", ObtenerDolarAsync)
            .WithTags("Cotización")
            .WithSummary("Consulta un servicio externo vía HttpClientFactory");

        app.MapGet("/api/demo/ciclo-de-vida", ObtenerCicloDeVida)
            .WithTags("Demo")
            .WithSummary("Muestra la diferencia entre Singleton, Scoped y Transient");

        return app;
    }

    private static async Task<Results<Ok<CotizacionDolar>, ProblemHttpResult>> ObtenerDolarAsync(
        ICotizacionService cotizacion, CancellationToken ct)
    {
        var dolar = await cotizacion.ObtenerDolarOficialAsync(ct);

        // Si la dependencia externa falla, respondemos 503 (servicio no disponible) en vez de 500.
        if (dolar is null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Servicio de cotización no disponible",
                detail: "Intentá de nuevo en unos minutos.");

        return TypedResults.Ok(dolar);
    }

    private static Ok<CicloDeVidaDto> ObtenerCicloDeVida(
        ISingletonId singleton,
        IScopedId scoped,
        ConsumidorDemo consumidor,
        ITransientId transient1,
        ITransientId transient2)
    {
        return TypedResults.Ok(new CicloDeVidaDto(
            singleton.Id,
            scoped.Id,
            consumidor.ScopedId,
            transient1.Id,
            transient2.Id));
    }
}
