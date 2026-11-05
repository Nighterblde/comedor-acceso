using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Domain.Accesos;
using Moq;

namespace Comedor.Application.Tests.Accesos;

public class MarcarServidoTests
{
    private static readonly DateTime Ahora = new(2026, 11, 10, 13, 30, 0);

    private static (MarcarServido Sut, Mock<IAccesoRepositorio> Accesos) Crear()
    {
        var accesos = new Mock<IAccesoRepositorio>();
        var reloj = new Mock<IReloj>();
        reloj.Setup(r => r.Ahora()).Returns(Ahora);
        return (new MarcarServido(accesos.Object, reloj.Object), accesos);
    }

    [Fact]
    public async Task ConsumoInexistente_LanzaKeyNotFound()
    {
        var (sut, accesos) = Crear();
        accesos.Setup(a => a.ObtenerConsumoAsync(It.IsAny<Guid>())).ReturnsAsync((Consumo?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.EjecutarAsync(Guid.NewGuid(), "chef"));
    }

    [Fact]
    public async Task PrimeraVez_RegistraEntrega_YaServidoFalso()
    {
        var (sut, accesos) = Crear();
        var consumo = Consumo.Crear("E1", new DateOnly(2026, 11, 10), "Comida", Ahora, "P1", Canal.Celular, null);
        accesos.Setup(a => a.ObtenerConsumoAsync(consumo.Id)).ReturnsAsync(consumo);

        var r = await sut.EjecutarAsync(consumo.Id, "chef");

        Assert.False(r.YaServido);
        Assert.Equal(Ahora, r.ServidoEn);
        accesos.Verify(a => a.RegistrarEntregaAsync(
            It.Is<Entrega>(e => e.ConsumoId == consumo.Id && e.Usuario == "chef" && e.FechaHora == Ahora)), Times.Once);
    }

    [Fact]
    public async Task EntregaDuplicada_YaServidoVerdadero()
    {
        var (sut, accesos) = Crear();
        var consumo = Consumo.Crear("E1", new DateOnly(2026, 11, 10), "Comida", Ahora, "P1", Canal.Celular, null);
        accesos.Setup(a => a.ObtenerConsumoAsync(consumo.Id)).ReturnsAsync(consumo);
        accesos.Setup(a => a.RegistrarEntregaAsync(It.IsAny<Entrega>())).ThrowsAsync(new EntregaDuplicadaException());

        var r = await sut.EjecutarAsync(consumo.Id, "chef");

        Assert.True(r.YaServido);
    }
}
