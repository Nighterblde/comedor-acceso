using Comedor.Api.Configuracion;
using Microsoft.Extensions.Configuration;

namespace Comedor.Api.Tests;

public class ConfiguracionTests
{
    [Theory]
    [InlineData("Jwt:Clave")]
    [InlineData("Qr:ClaveFirma")]
    [InlineData("ConnectionStrings:Comedor")]
    public void Requerido_SinValor_FallaNombrandoLaClave(string clave)
    {
        var config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => config.Requerido(clave));

        Assert.Contains(clave, ex.Message);
    }
}
