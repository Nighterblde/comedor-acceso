using Comedor.Domain.Accesos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class ConsumoConfiguracion : IEntityTypeConfiguration<Consumo>
{
    public void Configure(EntityTypeBuilder<Consumo> b)
    {
        b.ToTable("Consumos");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Matricula).HasMaxLength(20).IsRequired();
        b.Property(c => c.TiempoComida).HasMaxLength(50).IsRequired();
        b.Property(c => c.PuntoId).HasMaxLength(50).IsRequired();
        b.Property(c => c.Canal).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.Encargado).HasMaxLength(100);
        b.Property(c => c.Referencia).HasMaxLength(200);
        b.HasIndex(c => new { c.Matricula, c.FechaServicio, c.TiempoComida }).IsUnique();
        b.HasIndex(c => new { c.PuntoId, c.FechaHora });
    }
}
