namespace Comedor.Api.Tests;

public class EnsambladoTests
{
    [Fact]
    public void Api_ReferenciaAplicacionEInfraestructura() =>
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Comedor.Api.dll")));
}
