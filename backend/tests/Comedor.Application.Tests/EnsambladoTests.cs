namespace Comedor.Application.Tests;

public class EnsambladoTests
{
    [Fact]
    public void Aplicacion_SeCargaConSuNombre() =>
        Assert.Equal("Comedor.Application", typeof(EnsambladoAplicacion).Assembly.GetName().Name);
}
