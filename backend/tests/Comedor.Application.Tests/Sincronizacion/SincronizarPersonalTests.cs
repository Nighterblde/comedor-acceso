using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;
using Comedor.Domain.Empleados;

namespace Comedor.Application.Tests.Sincronizacion;

public class SincronizarPersonalTests
{
    private static readonly DateTime Hora = new(2026, 11, 4, 2, 0, 0);
    private static readonly IReadOnlySet<DayOfWeek> Lunes = new HashSet<DayOfWeek> { DayOfWeek.Monday };

    private sealed class RelojFalso : IReloj
    {
        public DateTime Ahora() => Hora;
    }

    private sealed class FuenteFalsa(IReadOnlyList<EmpleadoFuente>? filas, Exception? error = null) : IFuentePersonalRRHH
    {
        public Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync() =>
            error is not null ? throw error : Task.FromResult(filas!);
    }

    private sealed class RepoEmpleados(params Empleado[] iniciales) : IEmpleadoRepositorio
    {
        public List<Empleado> Guardados { get; } = [];
        public int Llamadas { get; private set; }

        public Task<Empleado?> ObtenerAsync(string matricula) =>
            Task.FromResult(iniciales.FirstOrDefault(e => e.Matricula == matricula));

        public Task<IReadOnlyList<Empleado>> TodosAsync() => Task.FromResult<IReadOnlyList<Empleado>>(iniciales);

        public Task GuardarAsync(IEnumerable<Empleado> empleados)
        {
            Llamadas++;
            Guardados.AddRange(empleados);
            return Task.CompletedTask;
        }
    }

    private sealed class RepoBitacora : IBitacoraSincronizacionRepositorio
    {
        public List<BitacoraSincronizacion> Filas { get; } = [];

        public Task RegistrarAsync(BitacoraSincronizacion fila)
        {
            Filas.Add(fila);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BitacoraSincronizacion>> UltimasAsync(int cantidad) =>
            Task.FromResult<IReadOnlyList<BitacoraSincronizacion>>(Filas.TakeLast(cantidad).Reverse().ToList());
    }

    private static EmpleadoFuente Fuente(string matricula, string turno = "Matutino", string nombre = "Ana Pérez") =>
        new(matricula, nombre, "Cocina", turno, Lunes, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null);

    private static Empleado Local(string matricula, string turno = "Matutino", bool activo = true,
        string nombre = "Ana Pérez") =>
        new(matricula, nombre, "Cocina", turno, Lunes, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, activo);

    private static SincronizarPersonal Crear(IFuentePersonalRRHH fuente, RepoEmpleados empleados, RepoBitacora bitacora) =>
        new(fuente, empleados, bitacora, new RelojFalso(), new OpcionesSincronizacion());

    [Fact]
    public async Task EmpleadoNuevo_CuentaComoAlta()
    {
        var repo = new RepoEmpleados();
        var r = await Crear(new FuenteFalsa([Fuente("a1")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.True(r.Exito);
        Assert.Equal(1, r.Altas);
        var guardado = Assert.Single(repo.Guardados);
        Assert.Equal("A1", guardado.Matricula);
        Assert.True(guardado.Activo);
    }

    [Fact]
    public async Task CambioDeTurno_CuentaComoCambio()
    {
        var repo = new RepoEmpleados(Local("A1", "Matutino"));
        var r = await Crear(new FuenteFalsa([Fuente("A1", "Nocturno")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.Equal(1, r.Cambios);
        Assert.Equal(0, r.Altas);
        Assert.Equal("Nocturno", Assert.Single(repo.Guardados).Turno);
    }

    [Fact]
    public async Task AusenteEnFuente_SeDaDeBaja()
    {
        var repo = new RepoEmpleados(Local("A1"), Local("A2"));
        var r = await Crear(new FuenteFalsa([Fuente("A1")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.Equal(1, r.Bajas);
        var baja = Assert.Single(repo.Guardados);
        Assert.Equal("A2", baja.Matricula);
        Assert.False(baja.Activo);
    }

    [Fact]
    public async Task MatriculaVacia_SeOmiteYContinua()
    {
        var repo = new RepoEmpleados();
        var r = await Crear(new FuenteFalsa([Fuente("  "), Fuente("A1")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.True(r.Exito);
        Assert.Equal(1, r.Omitidos);
        Assert.Equal(1, r.Altas);
    }

    [Fact]
    public async Task TurnoDesconocido_SeOmite()
    {
        var repo = new RepoEmpleados();
        var r = await Crear(new FuenteFalsa([Fuente("A1", "Madrugada")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.Equal(1, r.Omitidos);
        Assert.Equal(0, r.Altas);
        Assert.Equal(0, repo.Llamadas);
    }

    [Fact]
    public async Task FuenteCaida_NoModificaDatos_YRegistraError()
    {
        var repo = new RepoEmpleados(Local("A1"));
        var bitacora = new RepoBitacora();
        var r = await Crear(new FuenteFalsa(null, new InvalidOperationException("sin conexión")), repo, bitacora)
            .EjecutarAsync();

        Assert.False(r.Exito);
        Assert.Equal("sin conexión", r.Error);
        Assert.Equal(0, repo.Llamadas);
        var fila = Assert.Single(bitacora.Filas);
        Assert.False(fila.Exito);
        Assert.Equal("sin conexión", fila.Error);
    }

    [Fact]
    public async Task CadaEjecucion_EscribeBitacora()
    {
        var repo = new RepoEmpleados(Local("A2"), Local("A3", "Matutino"));
        var bitacora = new RepoBitacora();
        await Crear(new FuenteFalsa([Fuente("A1"), Fuente("A3", "Nocturno"), Fuente("")]), repo, bitacora)
            .EjecutarAsync();

        var fila = Assert.Single(bitacora.Filas);
        Assert.True(fila.Exito);
        Assert.Equal(Hora, fila.Fecha);
        Assert.Equal((1, 1, 1, 1), (fila.Altas, fila.Cambios, fila.Bajas, fila.Omitidos));
        Assert.Equal(1, repo.Llamadas);
    }

    [Fact]
    public async Task SinCambios_NoGuarda()
    {
        var repo = new RepoEmpleados(Local("A1"));
        var r = await Crear(new FuenteFalsa([Fuente("A1")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.True(r.Exito);
        Assert.Equal(0, repo.Llamadas);
    }

    [Fact]
    public async Task MatriculaRepetida_ConservaLaPrimera()
    {
        var repo = new RepoEmpleados();
        var r = await Crear(new FuenteFalsa([Fuente("A1", nombre: "Uno"), Fuente(" a1 ", nombre: "Dos")]), repo,
            new RepoBitacora()).EjecutarAsync();

        Assert.Equal(1, r.Altas);
        Assert.Equal(1, r.Omitidos);
        Assert.Equal("Uno", Assert.Single(repo.Guardados).Nombre);
    }

    [Fact]
    public async Task EmpleadoInactivoQueReaparece_SeReactivaComoCambio()
    {
        var repo = new RepoEmpleados(Local("A1", activo: false));
        var r = await Crear(new FuenteFalsa([Fuente("A1")]), repo, new RepoBitacora()).EjecutarAsync();

        Assert.Equal(1, r.Cambios);
        Assert.True(Assert.Single(repo.Guardados).Activo);
    }
}
