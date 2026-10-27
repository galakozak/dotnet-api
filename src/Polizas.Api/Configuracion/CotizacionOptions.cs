using System.ComponentModel.DataAnnotations;

namespace Polizas.Api.Configuracion;

/// <summary>
/// APRENDIZAJE (patrón Options): la configuración de appsettings.json se mapea a una clase tipada
/// en lugar de leer strings sueltos. Con ValidateOnStart() la app NO arranca si la configuración
/// es inválida: es mejor fallar al inicio que en el primer request en producción.
/// </summary>
public sealed class CotizacionOptions
{
    public const string Seccion = "Cotizacion";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSegundos { get; set; } = 5;
}
