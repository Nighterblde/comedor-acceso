using Comedor.Domain.Puntos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class PuntoEntradaConfiguracion : IEntityTypeConfiguration<PuntoEntrada>
{
    public void Configure(EntityTypeBuilder<PuntoEntrada> b)
    {
        b.ToTable("Puntos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasMaxLength(50);
        b.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
    }
}
