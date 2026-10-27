using Polizas.Api.Dtos;

namespace Polizas.Api.Services;

/// <summary>
/// APRENDIZAJE (SOLID, curso sección 18): los endpoints dependen de esta ABSTRACCIÓN, no de la clase concreta
/// (principio de inversión de dependencias). Eso permite reemplazar la implementación en los tests.
/// Todos los métodos son async y reciben un CancellationToken: si el cliente cancela el request,
/// la consulta a la base se cancela también (curso sección 14).
/// </summary>
public interface IPolizaService
{
    Task<PagedResult<PolizaDto>> ListarAsync(FiltroPolizas filtro, CancellationToken ct);
    Task<PolizaDto?> ObtenerAsync(int id, CancellationToken ct);
    Task<PolizaDto> CrearAsync(GuardarPolizaRequest request, CancellationToken ct);
    Task<PolizaDto?> ActualizarAsync(int id, GuardarPolizaRequest request, CancellationToken ct);
    Task<PolizaDto?> AnularAsync(int id, CancellationToken ct);
    Task<bool> EliminarAsync(int id, CancellationToken ct);
}
