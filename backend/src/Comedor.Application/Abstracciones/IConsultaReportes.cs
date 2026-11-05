using Comedor.Application.Reportes;
using Comedor.Domain.Accesos;

namespace Comedor.Application.Abstracciones;

/// <summary>Un consumo con el turno y el área del empleado ("Sin dato" si no existe) y si ya se sirvió.</summary>
public record ConsumoReporte(DateOnly FechaServicio, string TiempoComida, string Turno, string Area, bool Servido);

/// <summary>Consultas de solo lectura para los reportes; no modifica ningún registro.</summary>
public interface IConsultaReportes
{
    /// <summary>Consumos con fecha de servicio dentro del rango (inclusivo). Contingencia cuenta como servido.</summary>
    Task<IReadOnlyList<ConsumoReporte>> ConsumosAsync(DateOnly desde, DateOnly hasta);

    /// <summary>Consumos del rango con los filtros dados (matrícula ya normalizada).</summary>
    Task<IReadOnlyList<FilaDetalle>> ConsumosDetalleAsync(DateOnly desde, DateOnly hasta, string? matricula,
        string? puntoId, Canal? canal);

    /// <summary>Rechazos del rango; sin fecha de servicio se usa la fecha de la hora del rechazo.</summary>
    Task<IReadOnlyList<FilaDetalle>> RechazosDetalleAsync(DateOnly desde, DateOnly hasta, string? matricula,
        string? puntoId, Canal? canal, MotivoRechazo? motivo);
}
