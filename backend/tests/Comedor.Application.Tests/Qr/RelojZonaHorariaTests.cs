using Comedor.Infrastructure;

namespace Comedor.Application.Tests.Qr;

public class RelojZonaHorariaTests
{
    private sealed class ProveedorFijo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }

    [Fact]
    public void Ahora_ConvierteUtcAHoraDeHermosillo()
    {
        var utc = new DateTimeOffset(2026, 3, 2, 20, 30, 0, TimeSpan.Zero);
        var reloj = new RelojZonaHoraria("America/Hermosillo", new ProveedorFijo(utc));

        Assert.Equal(new DateTime(2026, 3, 2, 13, 30, 0), reloj.Ahora());
    }
}
