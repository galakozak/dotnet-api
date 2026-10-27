using System.Linq.Expressions;
using Polizas.Api.Domain;

namespace Polizas.Api.Dtos;

/// <summary>
/// APRENDIZAJE (DTOs): lo que la API devuelve. Es un record inmutable, separado de la entidad,
/// para poder cambiar la base de datos sin romper a los clientes de la API (y viceversa).
/// </summary>
public sealed record PolizaDto(
    int Id,
    string Numero,
    string Tomador,
    decimal SumaAsegurada,
    decimal Prima,
    DateOnly VigenciaDesde,
    DateOnly VigenciaHasta,
    EstadoPoliza Estado,
    DateTimeOffset CreadaEl,
    DateTimeOffset? AnuladaEl)
{
    /// <summary>
    /// APRENDIZAJE (LINQ + EF Core, curso secciones 15 y 17): usar una Expression en un .Select()
    /// hace que EF traiga de la base solo las columnas necesarias (una proyección).
    /// </summary>
    public static readonly Expression<Func<Poliza, PolizaDto>> Proyeccion = p => new PolizaDto(
        p.Id, p.Numero, p.Tomador, p.SumaAsegurada, p.Prima,
        p.VigenciaDesde, p.VigenciaHasta, p.Estado, p.CreadaEl, p.AnuladaEl);

    private static readonly Func<Poliza, PolizaDto> ProyeccionCompilada = Proyeccion.Compile();

    /// <summary>Para convertir una entidad que ya está en memoria (por ejemplo, recién creada).</summary>
    public static PolizaDto De(Poliza poliza) => ProyeccionCompilada(poliza);
}
