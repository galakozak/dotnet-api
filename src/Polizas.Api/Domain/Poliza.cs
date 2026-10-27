namespace Polizas.Api.Domain;

/// <summary>
/// APRENDIZAJE: la entidad es el modelo que se persiste con EF Core. NUNCA se expone
/// directamente en la API (para eso están los DTOs en /Dtos). La regla "no se puede anular
/// dos veces" vive acá, en el dominio, y no desparramada en los endpoints.
/// </summary>
public sealed class Poliza
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Tomador { get; set; } = string.Empty;
    public decimal SumaAsegurada { get; set; }
    public decimal Prima { get; set; }
    public DateOnly VigenciaDesde { get; set; }
    public DateOnly VigenciaHasta { get; set; }
    public EstadoPoliza Estado { get; set; } = EstadoPoliza.Vigente;
    public DateTimeOffset CreadaEl { get; set; }
    public DateTimeOffset? AnuladaEl { get; set; }

    public void Anular(DateTimeOffset ahora)
    {
        if (Estado == EstadoPoliza.Anulada)
            throw new ReglaDeNegocioException($"La póliza {Numero} ya está anulada.");

        Estado = EstadoPoliza.Anulada;
        AnuladaEl = ahora;
    }
}
