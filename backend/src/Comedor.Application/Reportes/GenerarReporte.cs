using Comedor.Application.Abstracciones;
using Comedor.Domain.Accesos;

namespace Comedor.Application.Reportes;

public class GenerarReporte(IConsultaReportes consulta)
{
    public async Task<IReadOnlyList<FilaConteo>> ConteoAsync(DateOnly desde, DateOnly hasta)
    {
        var consumos = await consulta.ConsumosAsync(desde, hasta);
        return consumos
            .GroupBy(c => (c.FechaServicio, c.TiempoComida, c.Turno, c.Area))
            .Select(g => new FilaConteo(g.Key.FechaServicio, g.Key.TiempoComida, g.Key.Turno, g.Key.Area, g.Count()))
            .OrderBy(f => f.FechaServicio).ThenBy(f => f.TiempoComida, StringComparer.Ordinal)
            .ThenBy(f => f.Turno, StringComparer.Ordinal).ThenBy(f => f.Area, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<FilaCotejo>> CotejoFacturaAsync(DateOnly desde, DateOnly hasta)
    {
        var consumos = await consulta.ConsumosAsync(desde, hasta);
        return consumos
            .GroupBy(c => (c.FechaServicio, c.TiempoComida))
            .Select(g => new FilaCotejo(g.Key.FechaServicio, g.Key.TiempoComida,
                g.Count(c => c.Servido), g.Count(c => !c.Servido)))
            .OrderBy(f => f.FechaServicio).ThenBy(f => f.TiempoComida, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<FilaDetalle>> DetalleAsync(DateOnly desde, DateOnly hasta, string? matricula,
        string? puntoId, Canal? canal, MotivoRechazo? motivo)
    {
        var mat = string.IsNullOrWhiteSpace(matricula) ? null : matricula.Trim().ToUpperInvariant();
        var punto = string.IsNullOrWhiteSpace(puntoId) ? null : puntoId.Trim();

        // Un filtro por motivo solo aplica a rechazos: los consumos no tienen motivo.
        IReadOnlyList<FilaDetalle> consumos = motivo is null
            ? await consulta.ConsumosDetalleAsync(desde, hasta, mat, punto, canal)
            : [];
        var rechazos = await consulta.RechazosDetalleAsync(desde, hasta, mat, punto, canal, motivo);
        return consumos.Concat(rechazos).OrderByDescending(f => f.FechaHora).ToList();
    }
}
