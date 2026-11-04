using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;

namespace Comedor.Domain.Reglas;

public static class EvaluadorDerecho
{
    public static ResultadoEvaluacion Evaluar(Empleado? empleado, IReadOnlyList<ReglaDerecho> reglas, DateTime ahora,
        Func<DateOnly, string, bool> yaConsumio)
    {
        if (empleado is null || !empleado.Activo)
        {
            return Rechazar(MotivoRechazo.NoRegistrado, null, null);
        }

        var hora = TimeOnly.FromDateTime(ahora);
        var regla = reglas.FirstOrDefault(r => r.Turno == empleado.Turno && r.Contiene(hora));
        var fecha = regla?.FechaServicio(ahora) ?? DateOnly.FromDateTime(ahora);

        if (fecha < empleado.InicioContrato || fecha > empleado.FinContrato)
        {
            return Rechazar(MotivoRechazo.ContratoVencido, regla, regla is null ? null : fecha);
        }

        if (!empleado.DiasLaborales.Contains(fecha.DayOfWeek))
        {
            return Rechazar(MotivoRechazo.DiaDescanso, regla, regla is null ? null : fecha);
        }

        if (!reglas.Any(r => r.Contiene(hora)))
        {
            return Rechazar(MotivoRechazo.FueraDeHorario, null, null);
        }

        if (regla is null)
        {
            return Rechazar(MotivoRechazo.TurnoSinDerecho, null, null);
        }

        if (yaConsumio(fecha, regla.TiempoComida))
        {
            return Rechazar(MotivoRechazo.YaConsumio, regla, fecha);
        }

        return new ResultadoEvaluacion(true, null, regla, fecha);
    }

    private static ResultadoEvaluacion Rechazar(MotivoRechazo motivo, ReglaDerecho? regla, DateOnly? fecha) =>
        new(false, motivo, regla, fecha);
}
