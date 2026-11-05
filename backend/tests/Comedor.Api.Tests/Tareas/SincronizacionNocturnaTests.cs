using Comedor.Api.Tareas;
using Comedor.Infrastructure.Externos;

namespace Comedor.Api.Tests.Tareas;

public class SincronizacionNocturnaTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 24)]
    [InlineData(3, 23)]
    public void TiempoHastaSiguiente_CalculaHastaLaProximaOcurrencia(int hora, int esperadoHoras)
    {
        var ahora = new DateTime(2026, 11, 4, hora, 0, 0);

        var espera = SincronizacionNocturna.TiempoHastaSiguiente(ahora, new TimeOnly(2, 0));

        Assert.Equal(TimeSpan.FromHours(esperadoHoras), espera);
    }

    [Fact]
    public async Task ArchivoSimulado_Tiene20Empleados_Y2ConContratoVencido()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "datos", "empleados-simulados.json");
        var filas = await new FuenteRRHHSimulada(ruta).ObtenerEmpleadosAsync();

        Assert.Equal(20, filas.Count);
        Assert.Equal(2, filas.Count(f => f.FinContrato < new DateOnly(2026, 11, 4)));
        Assert.Equal(20, filas.Select(f => f.Matricula).Distinct().Count());
        Assert.All(filas, f => Assert.Contains(f.Turno, new[] { "Matutino", "Vespertino", "Nocturno" }));
    }
}
