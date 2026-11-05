using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Application.Qr;
using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;
using Comedor.Domain.Puntos;
using Comedor.Domain.Reglas;
using Comedor.Infrastructure.Seguridad;
using Moq;

namespace Comedor.Application.Tests.Accesos;

public class ValidarAccesoTests
{
    private const string Clave = "clave-de-prueba-0123456789";
    private static readonly DateTime Lunes1330 = new(2026, 3, 2, 13, 30, 0);

    private sealed class RelojFalso : IReloj
    {
        public DateTime Valor { get; set; } = Lunes1330;

        public DateTime Ahora() => Valor;
    }

    private sealed class PuntoRepositorioFalso : IPuntoRepositorio
    {
        public Task<PuntoEntrada?> ObtenerAsync(string id) =>
            Task.FromResult<PuntoEntrada?>(id == "P1" ? new PuntoEntrada("P1", "Entrada principal", true) : null);

        public Task<IReadOnlyList<PuntoEntrada>> TodosAsync() =>
            Task.FromResult<IReadOnlyList<PuntoEntrada>>([]);

        public Task GuardarAsync(PuntoEntrada p) => Task.CompletedTask;
    }

    private sealed class AccesoRepositorioEnMemoria : IAccesoRepositorio
    {
        public List<Consumo> Consumos { get; } = [];
        public List<Rechazo> Rechazos { get; } = [];

        public Task<bool> YaConsumioAsync(string matricula, DateOnly fechaServicio, string tiempoComida) =>
            Task.FromResult(Consumos.Any(c =>
                c.Matricula == matricula && c.FechaServicio == fechaServicio && c.TiempoComida == tiempoComida));

        public async Task RegistrarConsumoAsync(Consumo c)
        {
            if (await YaConsumioAsync(c.Matricula, c.FechaServicio, c.TiempoComida))
            {
                throw new ConsumoDuplicadoException();
            }

            Consumos.Add(c);
        }

        public Task RegistrarRechazoAsync(Rechazo r)
        {
            Rechazos.Add(r);
            return Task.CompletedTask;
        }

        public Task RegistrarEntregaAsync(Entrega e) => throw new NotSupportedException();

        public Task<Consumo?> ObtenerConsumoAsync(Guid id) => throw new NotSupportedException();

        public Task<bool> EstaServidoAsync(Guid consumoId) => throw new NotSupportedException();

        public Task<IReadOnlyList<SolicitudPunto>> SolicitudesAsync(string puntoId, DateTime desde) =>
            throw new NotSupportedException();
    }

    private readonly RelojFalso _reloj = new();
    private readonly FirmaTokenHmac _firma = new(Clave);
    private readonly Mock<IEmpleadoRepositorio> _empleados = new();
    private readonly Mock<IReglaRepositorio> _reglas = new();
    private readonly Mock<IAccesoRepositorio> _accesos = new();

    private static readonly ReglaDerecho Comida = new("Matutino", "Comida", new TimeOnly(13, 0), new TimeOnly(15, 0));

    private static readonly IReadOnlySet<DayOfWeek> LunesAViernes = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    private static Empleado Matutino() => new(
        "AB123", "Ana Pérez", "Cocina", "Matutino", LunesAViernes,
        new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "http://foto/ab123.jpg", true);

    private ValidarAcceso Crear(IAccesoRepositorio? accesos = null, ReglaDerecho? regla = null)
    {
        _reglas.Setup(r => r.TodasAsync()).ReturnsAsync(new[] { regla ?? Comida });
        var validador = new ValidadorTokenQr(new PuntoRepositorioFalso(), _firma);
        return new ValidarAcceso(_empleados.Object, _reglas.Object, accesos ?? _accesos.Object, validador, _reloj);
    }

    private string TokenValido() => _firma.Firmar(new TokenQr("P1", _reloj.Ahora()));

    [Fact]
    public async Task Celular_TokenInvalido_EscanearDeNuevo_SinRegistros()
    {
        var caso = Crear();

        var r = await caso.PorCelularAsync("ab123", "basura");

        Assert.Equal(EstadoAcceso.EscanearDeNuevo, r.Estado);
        Assert.Equal("Escanea de nuevo el código de la entrada", r.Motivo);
        Assert.Equal("AB123", r.Matricula);
        Assert.Null(r.Nombre);
        Assert.Equal(Lunes1330, r.Hora);
        _accesos.Verify(a => a.RegistrarRechazoAsync(It.IsAny<Rechazo>()), Times.Never);
        _accesos.Verify(a => a.RegistrarConsumoAsync(It.IsAny<Consumo>()), Times.Never);
    }

    [Fact]
    public async Task Celular_Autorizado_RegistraConsumoCanalCelular()
    {
        _empleados.Setup(e => e.ObtenerAsync("AB123")).ReturnsAsync(Matutino());
        _accesos.Setup(a => a.YaConsumioAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        Consumo? registrado = null;
        _accesos.Setup(a => a.RegistrarConsumoAsync(It.IsAny<Consumo>()))
            .Callback<Consumo>(c => registrado = c).Returns(Task.CompletedTask);
        var caso = Crear();

        var r = await caso.PorCelularAsync("AB123", TokenValido());

        Assert.Equal(EstadoAcceso.Autorizado, r.Estado);
        Assert.Null(r.Motivo);
        Assert.Equal("Ana Pérez", r.Nombre);
        Assert.Equal("Matutino", r.Turno);
        Assert.Equal("Comida", r.TiempoComida);
        Assert.Equal("http://foto/ab123.jpg", r.FotoUrl);
        Assert.Equal(Lunes1330, r.Hora);
        Assert.NotNull(registrado);
        Assert.Equal("AB123", registrado!.Matricula);
        Assert.Equal(new DateOnly(2026, 3, 2), registrado.FechaServicio);
        Assert.Equal("Comida", registrado.TiempoComida);
        Assert.Equal("P1", registrado.PuntoId);
        Assert.Equal(Canal.Celular, registrado.Canal);
        Assert.Null(registrado.Encargado);
        Assert.Equal(Lunes1330, registrado.FechaHora);
    }

    [Fact]
    public async Task Manual_Autorizado_RegistraConsumoCanalManualConEncargado()
    {
        _empleados.Setup(e => e.ObtenerAsync("AB123")).ReturnsAsync(Matutino());
        Consumo? registrado = null;
        _accesos.Setup(a => a.RegistrarConsumoAsync(It.IsAny<Consumo>()))
            .Callback<Consumo>(c => registrado = c).Returns(Task.CompletedTask);
        var caso = Crear();

        var r = await caso.ManualAsync("AB123", "P7", "encargado1");

        Assert.Equal(EstadoAcceso.Autorizado, r.Estado);
        Assert.NotNull(registrado);
        Assert.Equal(Canal.Manual, registrado!.Canal);
        Assert.Equal("encargado1", registrado.Encargado);
        Assert.Equal("P7", registrado.PuntoId);
    }

    [Fact]
    public async Task Manual_MatriculaConEspaciosYMinusculas_SeNormaliza()
    {
        _empleados.Setup(e => e.ObtenerAsync("AB123")).ReturnsAsync(Matutino());
        var caso = Crear();

        var r = await caso.ManualAsync(" ab123 ", "P1", "enc");

        Assert.Equal(EstadoAcceso.Autorizado, r.Estado);
        Assert.Equal("AB123", r.Matricula);
        _empleados.Verify(e => e.ObtenerAsync("AB123"), Times.Once);
        _accesos.Verify(a => a.RegistrarConsumoAsync(It.Is<Consumo>(c => c.Matricula == "AB123")), Times.Once);
    }

    [Fact]
    public async Task Rechazo_RegistraMotivo()
    {
        _empleados.Setup(e => e.ObtenerAsync("XX999")).ReturnsAsync((Empleado?)null);
        Rechazo? registrado = null;
        _accesos.Setup(a => a.RegistrarRechazoAsync(It.IsAny<Rechazo>()))
            .Callback<Rechazo>(x => registrado = x).Returns(Task.CompletedTask);
        var caso = Crear();

        var r = await caso.ManualAsync("xx999", "P1", "enc");

        Assert.Equal(EstadoAcceso.Rechazado, r.Estado);
        Assert.Equal("No registrado", r.Motivo);
        Assert.Null(r.Nombre);
        Assert.NotNull(registrado);
        Assert.Equal(MotivoRechazo.NoRegistrado, registrado!.Motivo);
        Assert.Equal("XX999", registrado.Matricula);
        Assert.Equal(Canal.Manual, registrado.Canal);
        Assert.Equal("enc", registrado.Encargado);
        _accesos.Verify(a => a.RegistrarConsumoAsync(It.IsAny<Consumo>()), Times.Never);
    }

    [Fact]
    public async Task ConsumoDuplicado_DevuelveYaConsumio_YRegistraRechazo()
    {
        _empleados.Setup(e => e.ObtenerAsync("AB123")).ReturnsAsync(Matutino());
        _accesos.Setup(a => a.RegistrarConsumoAsync(It.IsAny<Consumo>())).ThrowsAsync(new ConsumoDuplicadoException());
        Rechazo? registrado = null;
        _accesos.Setup(a => a.RegistrarRechazoAsync(It.IsAny<Rechazo>()))
            .Callback<Rechazo>(x => registrado = x).Returns(Task.CompletedTask);
        var caso = Crear();

        var r = await caso.ManualAsync("AB123", "P1", "enc");

        Assert.Equal(EstadoAcceso.Rechazado, r.Estado);
        Assert.Equal("Ya consumiste este tiempo de comida", r.Motivo);
        Assert.Equal("Comida", r.TiempoComida);
        Assert.NotNull(registrado);
        Assert.Equal(MotivoRechazo.YaConsumio, registrado!.Motivo);
        Assert.Equal(new DateOnly(2026, 3, 2), registrado.FechaServicio);
        Assert.Equal("Comida", registrado.TiempoComida);
    }

    [Fact]
    public async Task CruceMedianoche_SegundoIntento_EsYaConsumio()
    {
        var nocturno = new Empleado("N1", "Noé", "Planta", "Nocturno", LunesAViernes,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 31), null, true);
        _empleados.Setup(e => e.ObtenerAsync("N1")).ReturnsAsync(nocturno);
        var regla = new ReglaDerecho("Nocturno", "N-Cena", new TimeOnly(23, 30), new TimeOnly(0, 30));
        var memoria = new AccesoRepositorioEnMemoria();
        var caso = Crear(memoria, regla);

        _reloj.Valor = new DateTime(2026, 11, 11, 0, 15, 0);
        var primero = await caso.ManualAsync("N1", "P1", "enc");
        _reloj.Valor = new DateTime(2026, 11, 11, 0, 20, 0);
        var segundo = await caso.ManualAsync("N1", "P1", "enc");

        Assert.Equal(EstadoAcceso.Autorizado, primero.Estado);
        var consumo = Assert.Single(memoria.Consumos);
        Assert.Equal(new DateOnly(2026, 11, 10), consumo.FechaServicio);
        Assert.Equal(EstadoAcceso.Rechazado, segundo.Estado);
        Assert.Equal("Ya consumiste este tiempo de comida", segundo.Motivo);
        var rechazo = Assert.Single(memoria.Rechazos);
        Assert.Equal(MotivoRechazo.YaConsumio, rechazo.Motivo);
        Assert.Equal(new DateOnly(2026, 11, 10), rechazo.FechaServicio);
    }
}
