using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;

namespace Comedor.Application.Tests.Sincronizacion;

public class ConsultarEstadoSincronizacionTests
{
    private sealed class BitacoraFalsa(params BitacoraSincronizacion[] filas) : IBitacoraSincronizacionRepositorio
    {
        public Task RegistrarAsync(BitacoraSincronizacion fila) => Task.CompletedTask;

        public Task<IReadOnlyList<BitacoraSincronizacion>> UltimasAsync(int cantidad) =>
            Task.FromResult<IReadOnlyList<BitacoraSincronizacion>>(
                filas.OrderByDescending(f => f.Fecha).Take(cantidad).ToList());

        public Task<BitacoraSincronizacion?> UltimaExitosaAsync() =>
            Task.FromResult(filas.Where(f => f.Exito).OrderByDescending(f => f.Fecha).FirstOrDefault());
    }

    private static BitacoraSincronizacion Fila(DateTime fecha, bool exito) =>
        new(Guid.NewGuid(), fecha, exito, 0, 0, 0, 0, exito ? null : "fuente caída");

    [Fact]
    public async Task UltimaFallida_DevuelveFechaDeLaUltimaExitosa()
    {
        var exitosa = new DateTime(2026, 11, 3, 2, 0, 0);
        var fallida = new DateTime(2026, 11, 4, 2, 0, 0);
        var caso = new ConsultarEstadoSincronizacion(new BitacoraFalsa(
            Fila(new DateTime(2026, 11, 2, 2, 0, 0), true), Fila(exitosa, true), Fila(fallida, false)));

        var estado = await caso.EjecutarAsync();

        Assert.True(estado.UltimaFallida);
        Assert.Equal(fallida, estado.UltimaEjecucion);
        Assert.Equal(exitosa, estado.UltimaExitosa);
    }

    [Fact]
    public async Task UltimaExitosa_NoEsFallida()
    {
        var fecha = new DateTime(2026, 11, 4, 2, 0, 0);
        var caso = new ConsultarEstadoSincronizacion(new BitacoraFalsa(
            Fila(fecha.AddDays(-1), false), Fila(fecha, true)));

        var estado = await caso.EjecutarAsync();

        Assert.False(estado.UltimaFallida);
        Assert.Equal(fecha, estado.UltimaExitosa);
    }

    [Fact]
    public async Task SinEjecuciones_NoEsFallida_YSinFechas()
    {
        var estado = await new ConsultarEstadoSincronizacion(new BitacoraFalsa()).EjecutarAsync();

        Assert.False(estado.UltimaFallida);
        Assert.Null(estado.UltimaEjecucion);
        Assert.Null(estado.UltimaExitosa);
    }
}
