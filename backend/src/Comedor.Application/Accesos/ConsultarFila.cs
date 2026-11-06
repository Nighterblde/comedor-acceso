using Comedor.Application.Abstracciones;
using Comedor.Domain.Reglas;

namespace Comedor.Application.Accesos;

public class ConsultarFila(IAccesoRepositorio accesos, IReglaRepositorio reglas, IReloj reloj)
{
    internal const int MaximoSiguientes = 3;
    internal static readonly TimeSpan Ventana = TimeSpan.FromHours(12);

    public async Task<FilaServicio> EjecutarAsync(string puntoId)
    {
        var ahora = reloj.Ahora();
        var filas = await accesos.SolicitudesAsync(puntoId, ahora - Ventana);
        var espera = Esperando(filas, await reglas.TodasAsync(), ahora);
        return new FilaServicio(espera.FirstOrDefault(), espera.Skip(1).Take(MaximoSiguientes).ToList(), espera.Count);
    }

    /// <summary>Autorizados sin servir del horario en curso, por hora y, a igual hora, por consumo (D-35).</summary>
    internal static IReadOnlyList<SolicitudPunto> Esperando(
        IReadOnlyList<SolicitudPunto> filas, IReadOnlyList<ReglaDerecho> reglas, DateTime ahora) =>
        filas
            .Where(f => f.Autorizado && !f.Servido && f.TiempoComida is not null && f.FechaServicio is not null
                && HorarioEnCurso.Contiene(reglas, ahora, f.Turno, f.TiempoComida, f.FechaServicio.Value))
            .OrderBy(f => f.FechaHora)
            .ThenBy(f => f.ConsumoId)
            .ToList();
}
