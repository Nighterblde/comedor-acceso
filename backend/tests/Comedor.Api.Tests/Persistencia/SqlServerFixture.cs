using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Comedor.Api.Tests.Persistencia;

/// <summary>Un solo contenedor de SQL Server compartido; aplica las migraciones al iniciar.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();

    public ComedorDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ComedorDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;
        return new ComedorDbContext(opciones);
    }
}

[CollectionDefinition("SqlServer")]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}
