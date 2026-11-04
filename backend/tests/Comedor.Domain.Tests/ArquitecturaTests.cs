using System.Reflection;

namespace Comedor.Domain.Tests;

public class ArquitecturaTests
{
    [Fact]
    public void Dominio_NoReferenciaOtrosEnsamblados() =>
        Assert.DoesNotContain(typeof(Comedor.Domain.Empleados.Empleado).Assembly.GetReferencedAssemblies(),
            a => a.Name!.StartsWith("Comedor.") || a.Name!.StartsWith("Microsoft.EntityFrameworkCore"));

    [Fact]
    public void Aplicacion_NoReferenciaInfraestructuraNiApi() =>
        Assert.DoesNotContain(typeof(Comedor.Application.EnsambladoAplicacion).Assembly.GetReferencedAssemblies(),
            a => a.Name!.StartsWith("Comedor.Infrastructure") || a.Name!.StartsWith("Comedor.Api"));
}
