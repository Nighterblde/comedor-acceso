using Comedor.Application.Abstracciones;
using Comedor.Application.Qr;
using Comedor.Domain.Puntos;
using Comedor.Infrastructure.Seguridad;

namespace Comedor.Application.Tests.Qr;

public class ValidadorTokenQrTests
{
    private static readonly DateTime Base = new(2026, 3, 2, 13, 0, 0);

    private sealed class RelojFalso : IReloj
    {
        public DateTime Valor { get; set; } = Base;

        public DateTime Ahora() => Valor;
    }

    private sealed class PuntoRepositorioFalso : IPuntoRepositorio
    {
        private readonly Dictionary<string, PuntoEntrada> _puntos = new();

        public PuntoRepositorioFalso(params PuntoEntrada[] puntos)
        {
            foreach (var p in puntos)
            {
                _puntos[p.Id] = p;
            }
        }

        public Task<PuntoEntrada?> ObtenerAsync(string id) =>
            Task.FromResult(_puntos.GetValueOrDefault(id));

        public Task<IReadOnlyList<PuntoEntrada>> TodosAsync() =>
            Task.FromResult<IReadOnlyList<PuntoEntrada>>(_puntos.Values.ToList());

        public Task GuardarAsync(PuntoEntrada p)
        {
            _puntos[p.Id] = p;
            return Task.CompletedTask;
        }
    }

    private const string Clave = "clave-de-prueba-0123456789";

    private readonly RelojFalso _reloj = new();
    private readonly FirmaTokenHmac _firma = new(Clave);
    private readonly PuntoRepositorioFalso _puntos = new(
        new PuntoEntrada("P1", "Entrada principal", true),
        new PuntoEntrada("P2", "Entrada cerrada", false));

    private ValidadorTokenQr CrearValidador() =>
        new(_puntos, _firma, _reloj, new OpcionesQr());

    private EmitirTokenQr CrearEmisor() => new(_puntos, _firma, _reloj);

    [Fact]
    public async Task TokenRecienEmitido_EsValido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");

        Assert.Equal("P1", await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenCon59Segundos_EsValido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");
        _reloj.Valor = Base.AddSeconds(59);

        Assert.Equal("P1", await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenCon60Segundos_EsValido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");
        _reloj.Valor = Base.AddSeconds(60);

        Assert.Equal("P1", await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenCon61Segundos_EsInvalido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");
        _reloj.Valor = Base.AddSeconds(61);

        Assert.Null(await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenFuturo_EsInvalido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");
        _reloj.Valor = Base.AddSeconds(-1);

        Assert.Null(await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenAlterado_EsInvalido()
    {
        var token = await CrearEmisor().EjecutarAsync("P1");
        var primero = token[0] == 'A' ? 'B' : 'A';
        var alterado = primero + token[1..];

        Assert.Null(await CrearValidador().ValidarAsync(alterado));
    }

    [Fact]
    public async Task TokenFirmadoConOtraClave_EsInvalido()
    {
        var otra = new FirmaTokenHmac("otra-clave-distinta-9876543210");
        var token = otra.Firmar(new TokenQr("P1", _reloj.Ahora()));

        Assert.Null(await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task TokenDePuntoInactivo_EsInvalido()
    {
        var token = _firma.Firmar(new TokenQr("P2", _reloj.Ahora()));

        Assert.Null(await CrearValidador().ValidarAsync(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("basura")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    public async Task TokenMalformado_EsInvalido(string token)
    {
        Assert.Null(await CrearValidador().ValidarAsync(token));
    }

    [Fact]
    public async Task Emitir_PuntoInactivo_Lanza()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CrearEmisor().EjecutarAsync("P2"));
    }

    [Fact]
    public async Task Emitir_PuntoInexistente_Lanza()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CrearEmisor().EjecutarAsync("NO"));
    }

    [Fact]
    public void FirmaTokenHmac_ClaveVacia_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new FirmaTokenHmac(""));
    }
}
