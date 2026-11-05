using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Domain.Reglas;
using Moq;

namespace Comedor.Application.Tests.Accesos;

public class ListarSolicitudesTests
{
    private static readonly DateTime Martes1330 = new(2026, 11, 10, 13, 30, 0);
    private static readonly DateOnly Hoy = new(2026, 11, 10);

    private static SolicitudPunto Fila(string matricula, bool autorizado, bool servido, string tiempo,
        DateOnly? fecha, DateTime hora) =>
        new(autorizado ? Guid.NewGuid() : null, hora, matricula, "Nombre", null, "Matutino", tiempo, fecha,
            autorizado, autorizado ? null : "Fuera de horario", servido);

    private static ListarSolicitudes Crear(IReadOnlyList<SolicitudPunto> filas, out Mock<IAccesoRepositorio> accesos)
    {
        accesos = new Mock<IAccesoRepositorio>();
        accesos.Setup(a => a.SolicitudesAsync(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(filas);
        var reglas = new Mock<IReglaRepositorio>();
        reglas.Setup(r => r.TodasAsync()).ReturnsAsync(new List<ReglaDerecho>
        {
            new("Matutino", "Comida", new TimeOnly(13, 0), new TimeOnly(15, 0)),
            new("Matutino", "Desayuno", new TimeOnly(7, 0), new TimeOnly(9, 0)),
        });
        var reloj = new Mock<IReloj>();
        reloj.Setup(r => r.Ahora()).Returns(Martes1330);
        return new ListarSolicitudes(accesos.Object, reglas.Object, reloj.Object);
    }

    [Fact]
    public async Task AutorizadoSinServirDeOtroHorario_NoSeDevuelve()
    {
        var enCurso = Fila("E1", true, false, "Comida", Hoy, Martes1330.AddMinutes(-5));
        var vencidoSinServir = Fila("E2", true, false, "Desayuno", Hoy, Martes1330.AddHours(-5));
        var vencidoServido = Fila("E3", true, true, "Desayuno", Hoy, Martes1330.AddHours(-5));
        var rechazo = Fila("E4", false, false, "Desayuno", Hoy, Martes1330.AddHours(-5));
        var sut = Crear([enCurso, vencidoSinServir, vencidoServido, rechazo], out _);

        var r = await sut.EjecutarAsync("P1", null);

        Assert.Equal(["E1", "E3", "E4"], r.Select(x => x.Matricula).ToArray());
    }

    [Fact]
    public async Task AutorizadoSinServirDeAyer_NoSeDevuelveAunqueElTiempoCoincida()
    {
        var ayer = Fila("E1", true, false, "Comida", Hoy.AddDays(-1), Martes1330.AddHours(-1));
        var sut = Crear([ayer], out _);

        Assert.Empty(await sut.EjecutarAsync("P1", null));
    }

    [Fact]
    public async Task SinDesde_ConsultaLasUltimasDosHoras_YOrdenaMasRecientePrimero()
    {
        var a = Fila("E1", true, false, "Comida", Hoy, Martes1330.AddMinutes(-20));
        var b = Fila("E2", true, false, "Comida", Hoy, Martes1330.AddMinutes(-5));
        var sut = Crear([a, b], out var accesos);

        var r = await sut.EjecutarAsync("P1", null);

        accesos.Verify(x => x.SolicitudesAsync("P1", Martes1330.AddHours(-2)), Times.Once);
        Assert.Equal(["E2", "E1"], r.Select(x => x.Matricula).ToArray());
    }
}
