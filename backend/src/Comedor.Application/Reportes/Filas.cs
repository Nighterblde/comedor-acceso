using Comedor.Domain.Accesos;

namespace Comedor.Application.Reportes;

public record FilaConteo(DateOnly FechaServicio, string TiempoComida, string Turno, string Area, int Total);

public record FilaCotejo(DateOnly FechaServicio, string TiempoComida, int Servidas, int SinServir);

public record FilaDetalle(DateTime FechaHora, string Matricula, string Nombre, string TiempoComida, string PuntoId,
    Canal Canal, string Resultado, string? Motivo, bool Servido);
