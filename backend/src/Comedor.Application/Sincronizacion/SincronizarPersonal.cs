using Comedor.Application.Abstracciones;
using Comedor.Domain.Empleados;

namespace Comedor.Application.Sincronizacion;

public class SincronizarPersonal(
    IFuentePersonalRRHH fuente,
    IEmpleadoRepositorio empleados,
    IBitacoraSincronizacionRepositorio bitacora,
    IReloj reloj,
    OpcionesSincronizacion opciones)
{
    public async Task<ResultadoSincronizacion> EjecutarAsync()
    {
        IReadOnlyList<EmpleadoFuente> filas;
        try
        {
            filas = await fuente.ObtenerEmpleadosAsync();
        }
        catch (Exception ex)
        {
            await bitacora.RegistrarAsync(
                new BitacoraSincronizacion(Guid.NewGuid(), reloj.Ahora(), false, 0, 0, 0, 0, ex.Message));
            return new ResultadoSincronizacion(false, 0, 0, 0, 0, ex.Message);
        }

        var locales = (await empleados.TodosAsync()).ToDictionary(e => e.Matricula);
        var validos = new Dictionary<string, EmpleadoFuente>();
        var omitidos = 0;
        foreach (var fila in filas)
        {
            var matricula = (fila.Matricula ?? string.Empty).Trim().ToUpperInvariant();
            if (matricula.Length == 0
                || !opciones.Turnos.Contains(fila.Turno, StringComparer.Ordinal)
                || !validos.TryAdd(matricula, fila with { Matricula = matricula }))
            {
                omitidos++;
            }
        }

        var porGuardar = new List<Empleado>();
        int altas = 0, cambios = 0, bajas = 0;
        foreach (var (matricula, f) in validos)
        {
            var nuevo = new Empleado(matricula, f.Nombre, f.Area, f.Turno, f.DiasLaborales, f.InicioContrato,
                f.FinContrato, f.FotoUrl, true);
            if (!locales.TryGetValue(matricula, out var actual))
            {
                altas++;
                porGuardar.Add(nuevo);
            }
            else if (Difiere(actual, nuevo))
            {
                cambios++;
                porGuardar.Add(nuevo);
            }
        }

        foreach (var local in locales.Values.Where(e => e.Activo && !validos.ContainsKey(e.Matricula)))
        {
            bajas++;
            porGuardar.Add(local with { Activo = false });
        }

        if (porGuardar.Count > 0)
        {
            await empleados.GuardarAsync(porGuardar);
        }

        await bitacora.RegistrarAsync(
            new BitacoraSincronizacion(Guid.NewGuid(), reloj.Ahora(), true, altas, cambios, bajas, omitidos, null));
        return new ResultadoSincronizacion(true, altas, cambios, bajas, omitidos, null);
    }

    private static bool Difiere(Empleado a, Empleado b) =>
        !a.Activo
        || a.Nombre != b.Nombre
        || a.Area != b.Area
        || a.Turno != b.Turno
        || !a.DiasLaborales.SetEquals(b.DiasLaborales)
        || a.InicioContrato != b.InicioContrato
        || a.FinContrato != b.FinContrato
        || a.FotoUrl != b.FotoUrl;
}
