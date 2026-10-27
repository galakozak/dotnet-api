namespace Polizas.Api.Demo;

/// <summary>
/// APRENDIZAJE (tiempos de vida en DI, curso sección 20): la MISMA clase registrada de tres formas.
///   Singleton -> una sola instancia para toda la vida de la aplicación.
///   Scoped    -> una instancia por request HTTP (el DbContext es Scoped).
///   Transient -> una instancia nueva cada vez que alguien la pide.
/// Llamá varias veces a GET /api/demo/ciclo-de-vida y mirá qué Guid cambia y cuál no.
/// </summary>
public interface IOperacionId { Guid Id { get; } }
public interface ISingletonId : IOperacionId { }
public interface IScopedId : IOperacionId { }
public interface ITransientId : IOperacionId { }

public sealed class OperacionId : ISingletonId, IScopedId, ITransientId
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>Otro servicio Scoped que depende de IScopedId: comparte la instancia con el endpoint dentro del mismo request.</summary>
public sealed class ConsumidorDemo(IScopedId scoped)
{
    public Guid ScopedId => scoped.Id;
}

public sealed record CicloDeVidaDto(
    Guid Singleton,
    Guid Scoped,
    Guid ScopedEnOtroServicio,
    Guid Transient1,
    Guid Transient2);
