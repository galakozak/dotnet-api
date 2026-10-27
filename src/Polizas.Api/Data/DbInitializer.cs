using Polizas.Api.Domain;

namespace Polizas.Api.Data;

/// <summary>Datos de ejemplo para poder probar la API apenas arranca (solo en Development).</summary>
public static class DbInitializer
{
    public static void Seed(AppDbContext db)
    {
        if (db.Polizas.Any()) return;

        var ahora = DateTimeOffset.UtcNow;

        db.Polizas.AddRange(
            Nueva("AUTO-0001", "Juan Pérez", 12_000_000m, 180_000m, new(2026, 1, 1), new(2027, 1, 1), ahora),
            Nueva("AUTO-0002", "María Gómez", 9_500_000m, 142_000m, new(2026, 3, 15), new(2027, 3, 15), ahora),
            Nueva("HOGAR-0001", "Carlos Rodríguez", 30_000_000m, 95_000m, new(2026, 6, 1), new(2027, 6, 1), ahora),
            Nueva("VIDA-0001", "Lucía Fernández", 50_000_000m, 210_000m, new(2025, 2, 1), new(2026, 2, 1), ahora, EstadoPoliza.Vencida),
            Nueva("COMERCIO-0001", "Panadería La Esquina SRL", 80_000_000m, 320_000m, new(2026, 5, 10), new(2027, 5, 10), ahora));

        db.SaveChanges();
    }

    private static Poliza Nueva(
        string numero, string tomador, decimal suma, decimal prima,
        DateOnly desde, DateOnly hasta, DateTimeOffset ahora,
        EstadoPoliza estado = EstadoPoliza.Vigente) => new()
    {
        Numero = numero,
        Tomador = tomador,
        SumaAsegurada = suma,
        Prima = prima,
        VigenciaDesde = desde,
        VigenciaHasta = hasta,
        Estado = estado,
        CreadaEl = ahora
    };
}
