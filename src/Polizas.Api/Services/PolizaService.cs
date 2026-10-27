using Microsoft.EntityFrameworkCore;
using Polizas.Api.Data;
using Polizas.Api.Domain;
using Polizas.Api.Dtos;

namespace Polizas.Api.Services;

/// <summary>
/// APRENDIZAJE (inyección de dependencias, curso sección 20): constructor primario (C# 12) que recibe
/// sus dependencias. Nadie hace "new PolizaService()": el contenedor de DI lo construye y le pasa
/// el DbContext, el reloj y el logger.
/// Se inyecta TimeProvider en vez de usar DateTime.UtcNow directo: así el tiempo se puede controlar en los tests.
/// </summary>
public sealed class PolizaService(AppDbContext db, TimeProvider reloj, ILogger<PolizaService> logger) : IPolizaService
{
    public async Task<PagedResult<PolizaDto>> ListarAsync(FiltroPolizas filtro, CancellationToken ct)
    {
        var pagina = Math.Max(1, filtro.Pagina);
        var tamanio = Math.Clamp(filtro.Tamanio, 1, 100);

        // AsNoTracking: para consultas de solo lectura, EF no necesita "seguir" los objetos (más rápido).
        IQueryable<Poliza> consulta = db.Polizas.AsNoTracking();

        // La consulta se arma de forma incremental y se ejecuta recién con CountAsync / ToListAsync
        // (ejecución diferida de LINQ, curso sección 15).
        if (filtro.Estado is { } estado)
            consulta = consulta.Where(p => p.Estado == estado);

        if (!string.IsNullOrWhiteSpace(filtro.Tomador))
        {
            var patron = $"%{filtro.Tomador.Trim()}%";
            consulta = consulta.Where(p => EF.Functions.Like(p.Tomador, patron));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(p => p.Id)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .Select(PolizaDto.Proyeccion)
            .ToListAsync(ct);

        return new PagedResult<PolizaDto>(items, total, pagina, tamanio);
    }

    public Task<PolizaDto?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Polizas
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PolizaDto.Proyeccion)
            .FirstOrDefaultAsync(ct);

    public async Task<PolizaDto> CrearAsync(GuardarPolizaRequest request, CancellationToken ct)
    {
        var numero = request.Numero.Trim();

        if (await db.Polizas.AnyAsync(p => p.Numero == numero, ct))
            throw new ReglaDeNegocioException($"Ya existe una póliza con el número {numero}.");

        var poliza = new Poliza
        {
            Numero = numero,
            Tomador = request.Tomador.Trim(),
            SumaAsegurada = request.SumaAsegurada,
            Prima = request.Prima,
            VigenciaDesde = request.VigenciaDesde,
            VigenciaHasta = request.VigenciaHasta,
            CreadaEl = reloj.GetUtcNow()
        };

        db.Polizas.Add(poliza);
        await db.SaveChangesAsync(ct);

        // APRENDIZAJE (logging estructurado): {Numero} y {Id} no son texto pegado, son propiedades
        // que se pueden filtrar y buscar en herramientas como Seq, Application Insights o Grafana.
        logger.LogInformation("Póliza {Numero} creada con id {Id}", poliza.Numero, poliza.Id);

        return PolizaDto.De(poliza);
    }

    public async Task<PolizaDto?> ActualizarAsync(int id, GuardarPolizaRequest request, CancellationToken ct)
    {
        var poliza = await db.Polizas.FindAsync([id], ct);
        if (poliza is null) return null;

        if (poliza.Estado == EstadoPoliza.Anulada)
            throw new ReglaDeNegocioException($"La póliza {poliza.Numero} está anulada y no puede modificarse.");

        var numero = request.Numero.Trim();

        if (await db.Polizas.AnyAsync(p => p.Numero == numero && p.Id != id, ct))
            throw new ReglaDeNegocioException($"Ya existe otra póliza con el número {numero}.");

        poliza.Numero = numero;
        poliza.Tomador = request.Tomador.Trim();
        poliza.SumaAsegurada = request.SumaAsegurada;
        poliza.Prima = request.Prima;
        poliza.VigenciaDesde = request.VigenciaDesde;
        poliza.VigenciaHasta = request.VigenciaHasta;

        // EF detecta los cambios sobre la entidad "trackeada" y genera el UPDATE solo.
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Póliza {Id} actualizada", id);
        return PolizaDto.De(poliza);
    }

    public async Task<PolizaDto?> AnularAsync(int id, CancellationToken ct)
    {
        var poliza = await db.Polizas.FindAsync([id], ct);
        if (poliza is null) return null;

        poliza.Anular(reloj.GetUtcNow()); // la regla de negocio vive en la entidad
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Póliza {Numero} anulada", poliza.Numero);
        return PolizaDto.De(poliza);
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct)
    {
        // ExecuteDeleteAsync: borra directo en la base sin traer la entidad a memoria.
        var borradas = await db.Polizas.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        return borradas > 0;
    }
}
