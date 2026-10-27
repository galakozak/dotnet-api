using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polizas.Api.Domain;

namespace Polizas.Api.Infrastructure;

/// <summary>
/// APRENDIZAJE (manejo de errores): un único lugar traduce excepciones a respuestas HTTP con el
/// formato estándar ProblemDetails (RFC 9457). Los endpoints no tienen try/catch.
///  - ReglaDeNegocioException -> 409 Conflict con el mensaje (es seguro mostrarlo).
///  - Cualquier otra         -> 500 SIN detalle (no filtrar información interna al cliente) y se loguea el error completo.
/// IExceptionHandler existe desde .NET 8.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var esReglaDeNegocio = exception is ReglaDeNegocioException;

        if (esReglaDeNegocio)
            logger.LogWarning("Regla de negocio incumplida: {Mensaje}", exception.Message);
        else
            logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);

        var status = esReglaDeNegocio ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = esReglaDeNegocio ? "Regla de negocio incumplida" : "Error inesperado",
                Detail = esReglaDeNegocio ? exception.Message : null
            }
        });
    }
}
