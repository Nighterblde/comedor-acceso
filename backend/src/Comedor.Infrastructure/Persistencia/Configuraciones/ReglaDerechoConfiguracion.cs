using Comedor.Domain.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class ReglaDerechoConfiguracion : IEntityTypeConfiguration<ReglaDerecho>
{
    public void Configure(EntityTypeBuilder<ReglaDerecho> b)
    {
        b.ToTable("Reglas");
        b.HasKey(r => new { r.Turno, r.TiempoComida });
        b.Property(r => r.Turno).HasMaxLength(50);
        b.Property(r => r.TiempoComida).HasMaxLength(50);
        b.Ignore(r => r.CruzaMedianoche);
    }
}
