using Comedor.Domain.Accesos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class EntregaConfiguracion : IEntityTypeConfiguration<Entrega>
{
    public void Configure(EntityTypeBuilder<Entrega> b)
    {
        b.ToTable("Entregas");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Usuario).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.ConsumoId).IsUnique();
    }
}
