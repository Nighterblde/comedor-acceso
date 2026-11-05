using Comedor.Application.Sincronizacion;
using Comedor.Infrastructure.Repositorios;

namespace Comedor.Api.Tests.Persistencia;

[Collection("SqlServer")]
public class BitacoraSincronizacionRepositorioTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Ultimas_DevuelveLasMasRecientesPrimero()
    {
        // fechas lejanas en el futuro para que sean las más recientes aunque otras pruebas inserten filas
        var baseFecha = new DateTime(2090, 1, 1, 2, 0, 0);
        await using var contexto = fixture.CrearContexto();
        var repo = new BitacoraSincronizacionRepositorio(contexto);
        for (var i = 0; i < 3; i++)
        {
            await repo.RegistrarAsync(
                new BitacoraSincronizacion(Guid.NewGuid(), baseFecha.AddDays(i), true, i, 0, 0, 0, null));
        }

        var ultimas = await repo.UltimasAsync(2);

        Assert.Equal(2, ultimas.Count);
        Assert.Equal(baseFecha.AddDays(2), ultimas[0].Fecha);
        Assert.Equal(baseFecha.AddDays(1), ultimas[1].Fecha);
    }
}
