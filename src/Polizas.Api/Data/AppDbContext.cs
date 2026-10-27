using Microsoft.EntityFrameworkCore;
using Polizas.Api.Domain;

namespace Polizas.Api.Data;

/// <summary>
/// APRENDIZAJE (EF Core, curso sección 17): el DbContext es la "unidad de trabajo".
/// Se registra como Scoped (uno por request HTTP) y NO es thread-safe: jamás se comparte entre requests.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Poliza> Polizas => Set<Poliza>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poliza>(e =>
        {
            e.HasKey(p => p.Id);

            // Índice único: la base es la última línea de defensa contra duplicados,
            // aunque el servicio también lo chequee para dar un mensaje claro.
            e.HasIndex(p => p.Numero).IsUnique();

            e.Property(p => p.Numero).HasMaxLength(20).IsRequired();
            e.Property(p => p.Tomador).HasMaxLength(120).IsRequired();
            e.Property(p => p.SumaAsegurada).HasPrecision(18, 2);
            e.Property(p => p.Prima).HasPrecision(18, 2);

            // El enum se guarda como texto ("Vigente") y no como número: más legible en la base.
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
        });
    }
}
