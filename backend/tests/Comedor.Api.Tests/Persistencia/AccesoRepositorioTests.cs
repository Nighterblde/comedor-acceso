using Comedor.Application.Abstracciones;
using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;
using Comedor.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Api.Tests.Persistencia;

[Collection("SqlServer")]
public class AccesoRepositorioTests(SqlServerFixture fixture)
{
    private static readonly DateOnly Fecha = new(2026, 3, 10);
    private static readonly DateTime Hora = new(2026, 3, 10, 13, 30, 0);

    private static string Unico(string prefijo) => prefijo + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static Consumo NuevoConsumo(string matricula, string punto = "P1") =>
        Consumo.Crear(matricula, Fecha, "Comida", Hora, punto, Canal.Celular, null);

    [Fact]
    public async Task RegistrarConsumo_DosVecesMismaClave_LanzaConsumoDuplicado()
    {
        var matricula = Unico("M");
        await using var contexto = fixture.CrearContexto();
        var repo = new AccesoRepositorio(contexto);
        await repo.RegistrarConsumoAsync(NuevoConsumo(matricula));

        await Assert.ThrowsAsync<ConsumoDuplicadoException>(() => repo.RegistrarConsumoAsync(NuevoConsumo(matricula)));

        // el contexto sigue siendo utilizable
        Assert.True(await repo.YaConsumioAsync(matricula, Fecha, "Comida"));
    }

    [Fact]
    public async Task YaConsumio_TrasRegistrar_EsVerdadero()
    {
        var matricula = Unico("M");
        await using var contexto = fixture.CrearContexto();
        var repo = new AccesoRepositorio(contexto);
        Assert.False(await repo.YaConsumioAsync(matricula, Fecha, "Comida"));

        await repo.RegistrarConsumoAsync(NuevoConsumo(matricula));

        Assert.True(await repo.YaConsumioAsync(matricula, Fecha, "Comida"));
    }

    [Fact]
    public async Task YaConsumio_OtraFechaServicio_EsFalso()
    {
        var matricula = Unico("M");
        await using var contexto = fixture.CrearContexto();
        var repo = new AccesoRepositorio(contexto);
        await repo.RegistrarConsumoAsync(NuevoConsumo(matricula));

        Assert.False(await repo.YaConsumioAsync(matricula, Fecha.AddDays(1), "Comida"));
    }

    [Fact]
    public async Task RegistrarEntrega_DosVeces_LanzaEntregaDuplicada()
    {
        await using var contexto = fixture.CrearContexto();
        var repo = new AccesoRepositorio(contexto);
        var consumo = NuevoConsumo(Unico("M"));
        await repo.RegistrarConsumoAsync(consumo);
        await repo.RegistrarEntregaAsync(Entrega.Crear(consumo.Id, Hora, "chef1"));

        await Assert.ThrowsAsync<EntregaDuplicadaException>(
            () => repo.RegistrarEntregaAsync(Entrega.Crear(consumo.Id, Hora, "chef2")));
        Assert.True(await repo.EstaServidoAsync(consumo.Id));
    }

    [Fact]
    public async Task Solicitudes_IncluyeConsumosYRechazosDelPunto_ConServido()
    {
        var punto = Unico("PUNTO");
        var otroPunto = Unico("OTRO");
        var servido = Unico("S");
        var pendiente = Unico("P");
        var desconocido = Unico("X");
        var antiguo = Unico("A");
        var ajeno = Unico("Z");

        await using var contexto = fixture.CrearContexto();
        await new EmpleadoRepositorio(contexto).GuardarAsync(
        [
            NuevoEmpleado(servido, "Ana Servida"),
            NuevoEmpleado(pendiente, "Pedro Pendiente")
        ]);

        var repo = new AccesoRepositorio(contexto);
        var t0 = new DateTime(2026, 3, 10, 13, 0, 0);
        var cServido = Consumo.Crear(servido, Fecha, "Comida", t0.AddMinutes(1), punto, Canal.Celular, null);
        var cPendiente = Consumo.Crear(pendiente, Fecha, "Comida", t0.AddMinutes(3), punto, Canal.Manual, "enc1");
        await repo.RegistrarConsumoAsync(cServido);
        await repo.RegistrarConsumoAsync(cPendiente);
        await repo.RegistrarEntregaAsync(Entrega.Crear(cServido.Id, t0.AddMinutes(2), "chef"));
        await repo.RegistrarRechazoAsync(Rechazo.Crear(desconocido, t0.AddMinutes(2), null, null, punto,
            Canal.Celular, null, MotivoRechazo.NoRegistrado));
        await repo.RegistrarConsumoAsync(Consumo.Crear(antiguo, Fecha, "Comida", t0.AddMinutes(-30), punto,
            Canal.Celular, null));
        await repo.RegistrarConsumoAsync(Consumo.Crear(ajeno, Fecha, "Comida", t0.AddMinutes(4), otroPunto,
            Canal.Celular, null));

        var lista = await repo.SolicitudesAsync(punto, t0);

        Assert.Equal(3, lista.Count);
        Assert.Equal([pendiente, desconocido, servido], lista.Select(s => s.Matricula).ToArray());

        var p = lista[0];
        Assert.Equal(cPendiente.Id, p.ConsumoId);
        Assert.Equal("Pedro Pendiente", p.Nombre);
        Assert.Equal("Matutino", p.Turno);
        Assert.Equal("Comida", p.TiempoComida);
        Assert.Equal(Fecha, p.FechaServicio);
        Assert.True(p.Autorizado);
        Assert.Null(p.Motivo);
        Assert.False(p.Servido);

        var r = lista[1];
        Assert.Null(r.ConsumoId);
        Assert.Null(r.Nombre);
        Assert.Null(r.Turno);
        Assert.False(r.Autorizado);
        Assert.Equal("No registrado", r.Motivo);
        Assert.False(r.Servido);

        var s = lista[2];
        Assert.Equal(cServido.Id, s.ConsumoId);
        Assert.Equal("Ana Servida", s.Nombre);
        Assert.True(s.Servido);
    }

    [Fact]
    public async Task DiasLaborales_IdaYVuelta()
    {
        var matricula = Unico("D");
        var dias = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
        await using (var escritura = fixture.CrearContexto())
        {
            await new EmpleadoRepositorio(escritura).GuardarAsync([NuevoEmpleado(matricula, "Dora", dias)]);
        }

        await using var lectura = fixture.CrearContexto();
        var leido = await new EmpleadoRepositorio(lectura).ObtenerAsync(matricula);

        Assert.NotNull(leido);
        Assert.True(dias.SetEquals(leido.DiasLaborales));
        var bits = await lectura.Database
            .SqlQuery<int>($"SELECT DiasLaborales AS Value FROM Empleados WHERE Matricula = {matricula}")
            .SingleAsync();
        Assert.Equal(1 + 4 + 16, bits);
    }

    [Fact]
    public async Task ModificarConsumo_LanzaInvalidOperation()
    {
        var consumo = NuevoConsumo(Unico("M"));
        await using (var escritura = fixture.CrearContexto())
        {
            await new AccesoRepositorio(escritura).RegistrarConsumoAsync(consumo);
        }

        await using var contexto = fixture.CrearContexto();
        var existente = await contexto.Consumos.SingleAsync(c => c.Id == consumo.Id);
        contexto.Entry(existente).Property(c => c.TiempoComida).CurrentValue = "Cena";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
        Assert.Equal("Los consumos, rechazos y entregas no se modifican ni se borran", ex.Message);
    }

    [Fact]
    public async Task BorrarEntrega_LanzaInvalidOperation()
    {
        var consumo = NuevoConsumo(Unico("M"));
        var entrega = Entrega.Crear(consumo.Id, Hora, "chef");
        await using (var escritura = fixture.CrearContexto())
        {
            var repo = new AccesoRepositorio(escritura);
            await repo.RegistrarConsumoAsync(consumo);
            await repo.RegistrarEntregaAsync(entrega);
        }

        await using var contexto = fixture.CrearContexto();
        var existente = await contexto.Entregas.SingleAsync(e => e.Id == entrega.Id);
        contexto.Entregas.Remove(existente);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
        Assert.Equal("Los consumos, rechazos y entregas no se modifican ni se borran", ex.Message);
    }

    private static Empleado NuevoEmpleado(string matricula, string nombre, IEnumerable<DayOfWeek>? dias = null) =>
        new(matricula, nombre, "Cocina", "Matutino",
            (dias ?? [DayOfWeek.Tuesday]).ToHashSet(),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "http://fotos/" + matricula, true);
}
