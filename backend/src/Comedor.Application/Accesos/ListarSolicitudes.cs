using Comedor.Application.Abstracciones;

namespace Comedor.Application.Accesos;

public class ListarSolicitudes(IAccesoRepositorio accesos, IReglaRepositorio reglas, IReloj reloj)
{
    private static readonly TimeSpan VentanaPorDefecto = TimeSpan.FromHours(2);

    /// <summary>
    /// Solicitudes del punto, más recientes primero. Un autorizado sin servir solo se muestra mientras su
    /// fecha de servicio y tiempo de comida sean los del horario en curso ("Por servir").
    /// </summary>
    public async Task<IReadOnlyList<SolicitudPunto>> EjecutarAsync(string puntoId, DateTime? desde)
    {
        var ahora = reloj.Ahora();
        var filas = await accesos.SolicitudesAsync(puntoId, desde ?? ahora - VentanaPorDefecto);
        var todas = await reglas.TodasAsync();

        return filas
            .Where(f => !f.Autorizado || f.Servido
                || (f.TiempoComida is not null && f.FechaServicio is not null
                    && HorarioEnCurso.Contiene(todas, ahora, f.Turno, f.TiempoComida, f.FechaServicio.Value)))
            .OrderByDescending(f => f.FechaHora)
            .ToList();
    }
}
