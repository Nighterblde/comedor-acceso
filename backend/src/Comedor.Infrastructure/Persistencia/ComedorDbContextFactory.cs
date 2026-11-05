using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Comedor.Infrastructure.Persistencia;

/// <summary>Solo para herramientas de diseño (dotnet ef); la cadena es un marcador sin secretos.</summary>
public class ComedorDbContextFactory : IDesignTimeDbContextFactory<ComedorDbContext>
{
    public ComedorDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ComedorDbContext>()
            .UseSqlServer("Server=localhost;Database=Comedor;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ComedorDbContext(opciones);
    }
}
