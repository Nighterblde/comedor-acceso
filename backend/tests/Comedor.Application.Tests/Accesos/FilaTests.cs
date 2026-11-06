using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Domain.Puntos;
using Comedor.Domain.Reglas;
using Moq;

namespace Comedor.Application.Tests.Accesos;

public class FilaTests
{
    private static readonly DateTime Martes1330 = new(2026, 11, 10, 13, 30, 0);
    private static readonly DateOnly Hoy = new(2026, 11, 10);

    private static SolicitudPunto Fila(string matricula, DateTime hora, Guid? id = null, bool servido = false,
        bool autorizado = true, string tiempo = "Comida", string turno = "Matutino") =>
        new(autorizado ? id ?? Guid.NewGuid() : null, hora, matricula, "Nombre", null, turno, tiempo, Hoy,
            autorizado, autorizado ? null : "Fuera de horario", servido);

    private static (ConsultarFila Fila, ConsultarMiTurno Turno) Crear(params SolicitudPunto[] filas) =>
        CrearA(Martes1330, filas);

    private static (ConsultarFila Fila, ConsultarMiTurno Turno) CrearA(DateTime ahora, params SolicitudPunto[] filas)
    {
        var accesos = new Mock<IAccesoRepositorio>();
        accesos.Setup(a => a.SolicitudesAsync("P1", It.IsAny<DateTime>())).ReturnsAsync(filas);
        accesos.Setup(a => a.SolicitudesAsync(It.Is<string>(p => p != "P1"), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<SolicitudPunto>());
        var reglas = new Mock<IReglaRepositorio>();
        reglas.Setup(r => r.TodasAsync()).ReturnsAsync(new List<ReglaDerecho>
        {
            new("Matutino", "Comida", new TimeOnly(13, 0), new TimeOnly(15, 0)),
            new("Matutino", "Desayuno", new TimeOnly(7, 0), new TimeOnly(9, 0)),
            new("Vespertino", "Comida", new TimeOnly(18, 0), new TimeOnly(20, 0)),
        });
        var puntos = new Mock<IPuntoRepositorio>();
        puntos.Setup(p => p.TodosAsync()).ReturnsAsync(new List<PuntoEntrada>
        {
            new("P0", "Otro", true), new("P1", "Comedor", true),
        });
        var reloj = new Mock<IReloj>();
        reloj.Setup(r => r.Ahora()).Returns(ahora);
        var fila = new ConsultarFila(accesos.Object, reglas.Object, reloj.Object);
        return (fila, new ConsultarMiTurno(accesos.Object, reglas.Object, puntos.Object, reloj.Object));
    }

    [Fact]
    public async Task Fila_OrdenPorLlegada()
    {
        var (sut, _) = Crear(
            Fila("E3", Martes1330.AddMinutes(-1)),
            Fila("E1", Martes1330.AddMinutes(-10)),
            Fila("E2", Martes1330.AddMinutes(-5)));

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("E1", r.Actual!.Matricula);
        Assert.Equal(["E2", "E3"], r.Siguientes.Select(x => x.Matricula).ToArray());
        Assert.Equal(3, r.Total);
    }

    [Fact]
    public async Task Fila_MismaHora_DesempataPorConsumoId()
    {
        var hora = Martes1330.AddMinutes(-3);
        var menor = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var mayor = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var (sut, _) = Crear(Fila("B", hora, mayor), Fila("A", hora, menor));

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("A", r.Actual!.Matricula);
        Assert.Equal("B", Assert.Single(r.Siguientes).Matricula);
    }

    [Fact]
    public async Task Fila_ExcluyeServidosYRechazos()
    {
        var (sut, _) = Crear(
            Fila("E1", Martes1330.AddMinutes(-9), servido: true),
            Fila("E2", Martes1330.AddMinutes(-8), autorizado: false),
            Fila("E3", Martes1330.AddMinutes(-7)));

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("E3", r.Actual!.Matricula);
        Assert.Empty(r.Siguientes);
        Assert.Equal(1, r.Total);
    }

    [Fact]
    public async Task Fila_SoloDelHorarioEnCurso()
    {
        var (sut, _) = Crear(
            Fila("DESAYUNO", Martes1330.AddHours(-5), tiempo: "Desayuno"),
            Fila("COMIDA", Martes1330.AddMinutes(-5)));

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("COMIDA", r.Actual!.Matricula);
        Assert.Equal(1, r.Total);
    }

    [Fact]
    public async Task Fila_OtroTurnoMismoTiempoDeComida_NoAparece()
    {
        var las1800 = Martes1330.Date.AddHours(18);
        var (sut, _) = CrearA(las1800,
            Fila("MATUTINO", Martes1330, turno: "Matutino"),
            Fila("VESPERTINO", las1800.AddMinutes(-5), turno: "Vespertino"));

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("VESPERTINO", r.Actual!.Matricula);
        Assert.Equal(1, r.Total);
    }

    [Fact]
    public async Task MiTurno_OtroTurnoMismoTiempoDeComida_SinTurno()
    {
        var las1800 = Martes1330.Date.AddHours(18);
        var (_, sut) = CrearA(las1800, Fila("MATUTINO", Martes1330, turno: "Matutino"));

        Assert.Equal(new MiTurno(EstadoTurno.SinTurno, null), await sut.EjecutarAsync("MATUTINO"));
    }

    [Fact]
    public async Task Fila_Vacia_ActualNulo()
    {
        var (sut, _) = Crear();

        var r = await sut.EjecutarAsync("P1");

        Assert.Null(r.Actual);
        Assert.Empty(r.Siguientes);
        Assert.Equal(0, r.Total);
    }

    [Fact]
    public async Task Fila_SiguientesMaximoTres_TotalCuentaTodos()
    {
        var filas = Enumerable.Range(1, 6)
            .Select(i => Fila($"E{i}", Martes1330.AddMinutes(-20 + i))).ToArray();
        var (sut, _) = Crear(filas);

        var r = await sut.EjecutarAsync("P1");

        Assert.Equal("E1", r.Actual!.Matricula);
        Assert.Equal(["E2", "E3", "E4"], r.Siguientes.Select(x => x.Matricula).ToArray());
        Assert.Equal(6, r.Total);
    }

    [Fact]
    public async Task MiTurno_Primero_Sirviendo()
    {
        var (_, sut) = Crear(Fila("E1", Martes1330.AddMinutes(-9)), Fila("E2", Martes1330.AddMinutes(-8)));

        var r = await sut.EjecutarAsync("E1");

        Assert.Equal(new MiTurno(EstadoTurno.Sirviendo, 1), r);
    }

    [Fact]
    public async Task MiTurno_Tercero_EnFilaPosicion3()
    {
        var (_, sut) = Crear(
            Fila("E1", Martes1330.AddMinutes(-9)),
            Fila("E2", Martes1330.AddMinutes(-8)),
            Fila("E3", Martes1330.AddMinutes(-7)));

        var r = await sut.EjecutarAsync("E3");

        Assert.Equal(new MiTurno(EstadoTurno.EnFila, 3), r);
    }

    [Fact]
    public async Task MiTurno_TrasServido_Servido()
    {
        var (_, sut) = Crear(
            Fila("E1", Martes1330.AddMinutes(-9), servido: true),
            Fila("E2", Martes1330.AddMinutes(-8)));

        var r = await sut.EjecutarAsync("E1");

        Assert.Equal(new MiTurno(EstadoTurno.Servido, null), r);
    }

    [Fact]
    public async Task MiTurno_SinConsumo_SinTurno()
    {
        var (_, sut) = Crear(Fila("E1", Martes1330.AddMinutes(-9)));

        Assert.Equal(new MiTurno(EstadoTurno.SinTurno, null), await sut.EjecutarAsync("E9"));
    }

    [Fact]
    public async Task MiTurno_ConsumoDelDesayuno_SinTurno()
    {
        var (_, sut) = Crear(Fila("E1", Martes1330.AddHours(-5), servido: true, tiempo: "Desayuno"));

        Assert.Equal(new MiTurno(EstadoTurno.SinTurno, null), await sut.EjecutarAsync("E1"));
    }
}
