using Microsoft.AspNetCore.Http.HttpResults;
using Polizas.Api.Dtos;
using Polizas.Api.Services;

namespace Polizas.Api.Endpoints;

/// <summary>
/// APRENDIZAJE (Minimal APIs + REST, curso secciones 19 y 23):
///  - Verbo = acción: GET lee, POST crea, PUT reemplaza, DELETE borra.
///  - La URL es un sustantivo en plural: /api/polizas y /api/polizas/{id}.
///  - Los códigos de estado comunican el resultado: 200, 201 (con Location), 204, 400, 404, 409.
///  - TypedResults + Results&lt;...&gt; declaran en el tipo de retorno TODAS las respuestas posibles.
///    El compilador valida que no devolvás otra cosa y OpenAPI documenta cada una automáticamente.
///  - Los handlers son métodos con nombre (no lambdas gigantes) para mantener legible el mapeo de rutas.
/// </summary>
public static class PolizasEndpoints
{
    public static IEndpointRouteBuilder MapPolizasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/polizas").WithTags("Pólizas");

        grupo.MapGet("/", ListarAsync)
            .WithSummary("Lista pólizas con filtros y paginación");

        grupo.MapGet("/{id:int}", ObtenerAsync)
            .WithSummary("Obtiene una póliza por id");

        grupo.MapPost("/", CrearAsync)
            .WithSummary("Crea una póliza");

        grupo.MapPut("/{id:int}", ActualizarAsync)
            .WithSummary("Reemplaza los datos de una póliza");

        grupo.MapPost("/{id:int}/anular", AnularAsync)
            .WithSummary("Anula una póliza (acción de negocio, no un borrado)");

        grupo.MapDelete("/{id:int}", EliminarAsync)
            .WithSummary("Elimina físicamente una póliza");

        return app;
    }

    // Los parámetros se resuelven solos según su origen: [AsParameters] y tipos simples vienen del
    // query/ruta, el record del body JSON, y IPolizaService / CancellationToken desde el contenedor de DI.
    private static async Task<Ok<PagedResult<PolizaDto>>> ListarAsync(
        [AsParameters] FiltroPolizas filtro, IPolizaService servicio, CancellationToken ct)
    {
        return TypedResults.Ok(await servicio.ListarAsync(filtro, ct));
    }

    private static async Task<Results<Ok<PolizaDto>, NotFound>> ObtenerAsync(
        int id, IPolizaService servicio, CancellationToken ct)
    {
        var poliza = await servicio.ObtenerAsync(id, ct);
        if (poliza is null) return TypedResults.NotFound();
        return TypedResults.Ok(poliza);
    }

    // La validación del body ocurre ANTES de entrar acá (AddValidation): si llegamos a esta línea, el request es válido.
    private static async Task<Created<PolizaDto>> CrearAsync(
        GuardarPolizaRequest request, IPolizaService servicio, CancellationToken ct)
    {
        var creada = await servicio.CrearAsync(request, ct);
        return TypedResults.Created($"/api/polizas/{creada.Id}", creada);
    }

    private static async Task<Results<Ok<PolizaDto>, NotFound>> ActualizarAsync(
        int id, GuardarPolizaRequest request, IPolizaService servicio, CancellationToken ct)
    {
        var actualizada = await servicio.ActualizarAsync(id, request, ct);
        if (actualizada is null) return TypedResults.NotFound();
        return TypedResults.Ok(actualizada);
    }

    private static async Task<Results<Ok<PolizaDto>, NotFound>> AnularAsync(
        int id, IPolizaService servicio, CancellationToken ct)
    {
        var anulada = await servicio.AnularAsync(id, ct);
        if (anulada is null) return TypedResults.NotFound();
        return TypedResults.Ok(anulada);
    }

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(
        int id, IPolizaService servicio, CancellationToken ct)
    {
        var eliminada = await servicio.EliminarAsync(id, ct);
        if (!eliminada) return TypedResults.NotFound();
        return TypedResults.NoContent();
    }
}
