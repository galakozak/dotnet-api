namespace Polizas.Api.Domain;

/// <summary>
/// APRENDIZAJE (curso sección 12 - excepciones): una excepción propia para expresar
/// "se violó una regla de negocio". El GlobalExceptionHandler la traduce a HTTP 409.
/// Las excepciones se reservan para casos excepcionales: "no encontrado" NO es una excepción,
/// el servicio devuelve null y el endpoint responde 404.
/// </summary>
public sealed class ReglaDeNegocioException(string mensaje) : Exception(mensaje);
