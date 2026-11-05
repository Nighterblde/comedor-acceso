using Comedor.Application.Abstracciones;
using Comedor.Application.Administracion;
using Comedor.Domain.Reglas;

namespace Comedor.Application.Tests.Administracion;

public class AdministrarReglasTests
{
    private sealed class RepoReglas(params ReglaDerecho[] iniciales) : IReglaRepositorio
    {
        public List<ReglaDerecho> Reglas { get; } = [.. iniciales];

        public Task<IReadOnlyList<ReglaDerecho>> TodasAsync() => Task.FromResult<IReadOnlyList<ReglaDerecho>>(Reglas.ToList());

        public Task GuardarAsync(ReglaDerecho r)
        {
            Reglas.RemoveAll(x => x.Turno == r.Turno && x.TiempoComida == r.TiempoComida);
            Reglas.Add(r);
            return Task.CompletedTask;
        }

        public Task EliminarAsync(string turno, string tiempoComida)
        {
            Reglas.RemoveAll(x => x.Turno == turno && x.TiempoComida == tiempoComida);
            return Task.CompletedTask;
        }
    }

    private static ReglaDerecho R(string turno, string tiempo, int hi, int hf, bool anterior = false) =>
        new(turno, tiempo, new TimeOnly(hi, 0), new TimeOnly(hf, 0), anterior);

    [Fact]
    public async Task Regla_InicioIgualFin_Lanza()
    {
        var repo = new RepoReglas();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            new AdministrarReglas(repo).GuardarAsync(R("Matutino", "Comida", 13, 13)));

        Assert.Equal("El inicio y el fin no pueden ser iguales", ex.Message);
        Assert.Empty(repo.Reglas);
    }

    [Fact]
    public async Task Regla_TraslapadaEnMismoTurno_Lanza()
    {
        var repo = new RepoReglas(R("Matutino", "Comida", 13, 15));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            new AdministrarReglas(repo).GuardarAsync(R("Matutino", "Cena", 14, 16)));

        Assert.Equal("Se traslapa con Comida de 13:00 a 15:00", ex.Message);
        Assert.Single(repo.Reglas);
    }

    [Fact]
    public async Task Regla_MismoHorarioEnOtroTurno_SeGuarda()
    {
        var repo = new RepoReglas(R("Matutino", "Comida", 13, 15));

        await new AdministrarReglas(repo).GuardarAsync(R("Vespertino", "Comida", 13, 15));

        Assert.Equal(2, repo.Reglas.Count);
    }

    [Fact]
    public async Task Regla_EditarLaMismaRegla_NoSeTraslapaConSigoMisma()
    {
        var repo = new RepoReglas(R("Matutino", "Comida", 13, 15));

        await new AdministrarReglas(repo).GuardarAsync(R("Matutino", "Comida", 13, 16));

        Assert.Equal(new TimeOnly(16, 0), Assert.Single(repo.Reglas).Fin);
    }

    [Fact]
    public async Task Regla_CruceMedianoche_SeGuarda()
    {
        var repo = new RepoReglas();

        await new AdministrarReglas(repo).GuardarAsync(R("Nocturno", "Cena", 23, 1));

        Assert.True(Assert.Single(repo.Reglas).CruzaMedianoche);
    }

    [Fact]
    public async Task Regla_JornadaAnterior_SeGuarda()
    {
        var repo = new RepoReglas();

        await new AdministrarReglas(repo).GuardarAsync(R("Nocturno", "Desayuno", 6, 8, anterior: true));

        Assert.True(Assert.Single(repo.Reglas).PerteneceAJornadaAnterior);
    }

    [Fact]
    public async Task Regla_Eliminar_Inexistente_DevuelveFalse()
    {
        var repo = new RepoReglas(R("Matutino", "Comida", 13, 15));
        var uc = new AdministrarReglas(repo);

        Assert.False(await uc.EliminarAsync("Matutino", "Cena"));
        Assert.True(await uc.EliminarAsync("Matutino", "Comida"));
        Assert.Empty(repo.Reglas);
    }
}
