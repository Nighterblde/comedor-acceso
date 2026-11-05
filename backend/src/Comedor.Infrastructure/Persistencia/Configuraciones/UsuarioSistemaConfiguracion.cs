using Comedor.Application.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class UsuarioSistemaConfiguracion : IEntityTypeConfiguration<UsuarioSistema>
{
    public void Configure(EntityTypeBuilder<UsuarioSistema> b)
    {
        b.ToTable("UsuariosSistema");
        b.HasKey(x => x.Usuario);
        b.Property(x => x.Usuario).HasMaxLength(64);
        b.Property(x => x.HashContrasena).HasMaxLength(512).IsRequired();
        b.Property(x => x.Rol).HasMaxLength(32).IsRequired();
    }
}
