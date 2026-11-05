using Comedor.Domain.Accesos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class RechazoConfiguracion : IEntityTypeConfiguration<Rechazo>
{
    public void Configure(EntityTypeBuilder<Rechazo> b)
    {
        b.ToTable("Rechazos");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Matricula).HasMaxLength(20).IsRequired();
        b.Property(r => r.TiempoComida).HasMaxLength(50);
        b.Property(r => r.PuntoId).HasMaxLength(50).IsRequired();
        b.Property(r => r.Canal).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.Encargado).HasMaxLength(100);
        b.Property(r => r.Motivo).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(r => new { r.PuntoId, r.FechaHora });
    }
}
