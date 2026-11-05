using Comedor.Application.Sincronizacion;
using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;
using Comedor.Domain.Puntos;
using Comedor.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Persistencia;

public class ComedorDbContext(DbContextOptions<ComedorDbContext> opciones) : DbContext(opciones)
{
    public const string MensajeSoloInsercion = "Los consumos, rechazos y entregas no se modifican ni se borran";

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<ReglaDerecho> Reglas => Set<ReglaDerecho>();
    public DbSet<PuntoEntrada> Puntos => Set<PuntoEntrada>();
    public DbSet<Consumo> Consumos => Set<Consumo>();
    public DbSet<Rechazo> Rechazos => Set<Rechazo>();
    public DbSet<Entrega> Entregas => Set<Entrega>();
    public DbSet<BitacoraSincronizacion> Bitacora => Set<BitacoraSincronizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComedorDbContext).Assembly);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        VerificarSoloInsercion();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        VerificarSoloInsercion();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void VerificarSoloInsercion()
    {
        ChangeTracker.DetectChanges();
        var viola = ChangeTracker.Entries().Any(e =>
            e.Entity is Consumo or Rechazo or Entrega
            && e.State is EntityState.Modified or EntityState.Deleted);
        if (viola)
        {
            throw new InvalidOperationException(MensajeSoloInsercion);
        }
    }
}
