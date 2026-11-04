using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;
using Comedor.Domain.Reglas;

namespace Comedor.Domain.Tests;

public class EvaluadorDerechoTests
{
    private static readonly IReadOnlySet<DayOfWeek> LunesAViernes = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    private static readonly IReadOnlyList<ReglaDerecho> Reglas = new[]
    {
        new ReglaDerecho("Matutino", "Comida", new TimeOnly(13, 0), new TimeOnly(15, 0)),
        new ReglaDerecho("Vespertino", "Cena", new TimeOnly(19, 0), new TimeOnly(21, 0)),
        new ReglaDerecho("Nocturno", "Desayuno", new TimeOnly(6, 0), new TimeOnly(7, 30), PerteneceAJornadaAnterior: true),
        new ReglaDerecho("Nocturno", "Cena", new TimeOnly(23, 30), new TimeOnly(0, 30)),
    };

    private static Empleado Emp(string turno, DateOnly? fin = null, bool activo = true) =>
        new("A001", "Ana", "Cocina", turno, LunesAViernes,
            new DateOnly(2026, 8, 1), fin ?? new DateOnly(2026, 12, 31), null, activo);

    private static ResultadoEvaluacion Eval(Empleado? e, DateTime ahora, bool yaConsumio = false) =>
        EvaluadorDerecho.Evaluar(e, Reglas, ahora, (_, _) => yaConsumio);

    [Fact]
    public void Autoriza_MatutinoEnComida()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 10, 13, 30, 0));
        Assert.True(r.Autorizado);
        Assert.Null(r.Motivo);
        Assert.Equal(new DateOnly(2026, 11, 10), r.FechaServicio);
    }

    [Fact]
    public void Rechaza_NoRegistrado()
    {
        var r = Eval(null, new DateTime(2026, 11, 10, 13, 30, 0));
        Assert.False(r.Autorizado);
        Assert.Equal(MotivoRechazo.NoRegistrado, r.Motivo);
    }

    [Fact]
    public void Rechaza_EmpleadoInactivo()
    {
        var r = Eval(Emp("Matutino", activo: false), new DateTime(2026, 11, 10, 13, 30, 0));
        Assert.Equal(MotivoRechazo.NoRegistrado, r.Motivo);
    }

    [Fact]
    public void Rechaza_ContratoVencido()
    {
        var r = Eval(Emp("Matutino", new DateOnly(2026, 11, 9)), new DateTime(2026, 11, 10, 13, 30, 0));
        Assert.Equal(MotivoRechazo.ContratoVencido, r.Motivo);
    }

    [Fact]
    public void ContratoQueVenceHoy_EsVigente()
    {
        var r = Eval(Emp("Matutino", new DateOnly(2026, 11, 10)), new DateTime(2026, 11, 10, 13, 30, 0));
        Assert.True(r.Autorizado);
    }

    [Fact]
    public void Rechaza_DiaDescanso()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 15, 13, 30, 0));
        Assert.Equal(MotivoRechazo.DiaDescanso, r.Motivo);
    }

    [Fact]
    public void Rechaza_FueraDeHorario()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 10, 16, 0, 0));
        Assert.Equal(MotivoRechazo.FueraDeHorario, r.Motivo);
    }

    [Fact]
    public void FinEsExclusivo()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 10, 15, 0, 0));
        Assert.Equal(MotivoRechazo.FueraDeHorario, r.Motivo);
    }

    [Fact]
    public void Rechaza_TurnoSinDerecho()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 10, 19, 30, 0));
        Assert.Equal(MotivoRechazo.TurnoSinDerecho, r.Motivo);
    }

    [Fact]
    public void Rechaza_YaConsumio()
    {
        var r = Eval(Emp("Matutino"), new DateTime(2026, 11, 10, 13, 30, 0), yaConsumio: true);
        Assert.Equal(MotivoRechazo.YaConsumio, r.Motivo);
        Assert.Equal(new DateOnly(2026, 11, 10), r.FechaServicio);
    }

    [Fact]
    public void Nocturno_DesayunoEsDeLaJornadaAnterior()
    {
        var r = Eval(Emp("Nocturno"), new DateTime(2026, 11, 11, 6, 15, 0));
        Assert.True(r.Autorizado);
        Assert.Equal(new DateOnly(2026, 11, 10), r.FechaServicio);
    }

    [Fact]
    public void Nocturno_DesayunoTrasUltimaJornada_DiaSiguienteDescanso()
    {
        var r = Eval(Emp("Nocturno"), new DateTime(2026, 11, 14, 6, 15, 0));
        Assert.True(r.Autorizado);
        Assert.Equal(new DateOnly(2026, 11, 13), r.FechaServicio);
    }

    [Fact]
    public void CruceMedianoche_DespuesDeMedianoche()
    {
        var r = Eval(Emp("Nocturno"), new DateTime(2026, 11, 11, 0, 15, 0));
        Assert.True(r.Autorizado);
        Assert.Equal(new DateOnly(2026, 11, 10), r.FechaServicio);
    }

    [Fact]
    public void CruceMedianoche_AntesDeMedianoche()
    {
        var r = Eval(Emp("Nocturno"), new DateTime(2026, 11, 10, 23, 45, 0));
        Assert.True(r.Autorizado);
        Assert.Equal(new DateOnly(2026, 11, 10), r.FechaServicio);
    }

    [Theory]
    [InlineData(MotivoRechazo.NoRegistrado, "No registrado")]
    [InlineData(MotivoRechazo.ContratoVencido, "Contrato vencido")]
    [InlineData(MotivoRechazo.DiaDescanso, "Hoy es tu día de descanso")]
    [InlineData(MotivoRechazo.FueraDeHorario, "Fuera de horario")]
    [InlineData(MotivoRechazo.TurnoSinDerecho, "Tu turno no tiene derecho a este tiempo de comida")]
    [InlineData(MotivoRechazo.YaConsumio, "Ya consumiste este tiempo de comida")]
    public void Textos_DeMotivo(MotivoRechazo motivo, string esperado) =>
        Assert.Equal(esperado, MotivoRechazoTexto.De(motivo));
}
