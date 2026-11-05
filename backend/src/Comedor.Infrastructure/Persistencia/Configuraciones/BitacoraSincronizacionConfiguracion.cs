using Comedor.Application.Sincronizacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class BitacoraSincronizacionConfiguracion : IEntityTypeConfiguration<BitacoraSincronizacion>
{
    public void Configure(EntityTypeBuilder<BitacoraSincronizacion> b)
    {
        b.ToTable("BitacoraSincronizacion");
        b.HasKey(x => x.Id);
        b.Property(x => x.Error).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.Fecha);
    }
}
