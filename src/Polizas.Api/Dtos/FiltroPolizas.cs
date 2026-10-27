using Polizas.Api.Domain;

namespace Polizas.Api.Dtos;

/// <summary>
/// APRENDIZAJE (REST - filtros y paginación): parámetros de query string agrupados con [AsParameters].
/// Ejemplo: GET /api/polizas?estado=Vigente&amp;tomador=perez&amp;pagina=2&amp;tamanio=5
/// Los límites (página mínima 1, tamaño máximo 100) se aplican en el servicio:
/// nunca hay que confiar en que el cliente mande valores razonables.
/// </summary>
public sealed record FiltroPolizas(
    EstadoPoliza? Estado = null,
    string? Tomador = null,
    int Pagina = 1,
    int Tamanio = 10);
