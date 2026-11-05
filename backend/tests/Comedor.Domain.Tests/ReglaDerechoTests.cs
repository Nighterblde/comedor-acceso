using Comedor.Domain.Reglas;

namespace Comedor.Domain.Tests;

public class ReglaDerechoTests
{
    private static readonly TimeOnly H13 = new(13, 0);

    [Fact]
    public void SeTraslapa_MismoTurno()
    {
        var a = new ReglaDerecho("Matutino", "Comida", H13, new TimeOnly(15, 0));
        var b = new ReglaDerecho("Matutino", "Comida", new TimeOnly(14, 0), new TimeOnly(16, 0));
        Assert.True(a.SeTraslapaCon(b));
    }

    [Fact]
    public void NoSeTraslapa_OtroTurno()
    {
        var a = new ReglaDerecho("Matutino", "Comida", H13, new TimeOnly(15, 0));
        var b = new ReglaDerecho("Vespertino", "Comida", new TimeOnly(14, 0), new TimeOnly(16, 0));
        Assert.False(a.SeTraslapaCon(b));
    }

    [Fact]
    public void NoSeTraslapa_Contiguas()
    {
        var a = new ReglaDerecho("Matutino", "Comida", H13, new TimeOnly(15, 0));
        var b = new ReglaDerecho("Matutino", "Cena", new TimeOnly(15, 0), new TimeOnly(16, 0));
        Assert.False(a.SeTraslapaCon(b));
    }

    [Fact]
    public void SeTraslapa_ConCruceDeMedianoche()
    {
        var a = new ReglaDerecho("Nocturno", "Cena", new TimeOnly(23, 30), new TimeOnly(0, 30));
        var b = new ReglaDerecho("Nocturno", "Desayuno", new TimeOnly(0, 15), new TimeOnly(1, 0));
        var c = new ReglaDerecho("Nocturno", "Desayuno", new TimeOnly(0, 30), new TimeOnly(1, 0));
        Assert.True(a.SeTraslapaCon(b));
        Assert.False(a.SeTraslapaCon(c));
    }

    [Fact]
    public void Contiene_FinExclusivo_YCruce()
    {
        var n = new ReglaDerecho("Nocturno", "Cena", new TimeOnly(23, 30), new TimeOnly(0, 30));
        Assert.True(n.CruzaMedianoche);
        Assert.True(n.Contiene(new TimeOnly(23, 30)));
        Assert.True(n.Contiene(new TimeOnly(0, 29)));
        Assert.False(n.Contiene(new TimeOnly(0, 30)));
        Assert.False(n.Contiene(new TimeOnly(12, 0)));
    }
}
