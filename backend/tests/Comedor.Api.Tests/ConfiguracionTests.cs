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

    [Fact]
    public void FuenteTipoDesconocido_FallaAlIniciar()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Clave"] = "clave-jwt-de-pruebas-con-mas-de-32-caracteres",
            ["Qr:ClaveFirma"] = "clave-firma-qr-de-pruebas-con-mas-de-32-caracteres",
            ["ConnectionStrings:Comedor"] = "Server=x",
            ["Fuente:Tipo"] = "Oracle",
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddServiciosComedor(config));

        Assert.Contains("Fuente:Tipo", ex.Message);
    }
}
