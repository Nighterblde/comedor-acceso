namespace Comedor.Application.Accesos;

public record SolicitudPunto(
    Guid? ConsumoId,
    DateTime FechaHora,
    string Matricula,
    string? Nombre,
    string? FotoUrl,
    string? Turno,
    string? TiempoComida,
    DateOnly? FechaServicio,
    bool Autorizado,
    string? Motivo,
    bool Servido);
