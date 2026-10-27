using System.ComponentModel.DataAnnotations;

namespace Polizas.Api.Dtos;

/// <summary>
/// APRENDIZAJE (validación): en .NET 10 las Minimal APIs validan solas los DTOs si se llama a
/// builder.Services.AddValidation() en Program.cs. Si falla, la API responde 400 con un
/// ProblemDetails que lista los errores por campo. Dos niveles de validación:
///  1) Atributos (DataAnnotations) para reglas de un solo campo.
///  2) IValidatableObject para reglas que cruzan campos (fechas, prima vs suma asegurada).
/// Nota: se usa Range(double, double) y no Range(typeof(decimal), "0.01", ...) porque esa forma
/// parsea el texto con la cultura del servidor (en es-AR "0.01" rompería).
/// </summary>
public sealed record GuardarPolizaRequest : IValidatableObject
{
    [Required, StringLength(20, MinimumLength = 3)]
    public string Numero { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string Tomador { get; init; } = string.Empty;

    [Range(1, 1_000_000_000_000d)]
    public decimal SumaAsegurada { get; init; }

    [Range(0.01, 1_000_000_000_000d)]
    public decimal Prima { get; init; }

    public DateOnly VigenciaDesde { get; init; }
    public DateOnly VigenciaHasta { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VigenciaHasta <= VigenciaDesde)
            yield return new ValidationResult(
                "La vigencia hasta debe ser posterior a la vigencia desde.",
                [nameof(VigenciaHasta)]);

        if (Prima > SumaAsegurada)
            yield return new ValidationResult(
                "La prima no puede superar la suma asegurada.",
                [nameof(Prima)]);
    }
}
