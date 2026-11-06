namespace Comedor.Application.Accesos;

/// <summary>Autorizados sin servir del horario en curso: el primero, los 3 siguientes y cuántos esperan en total.</summary>
public record FilaServicio(SolicitudPunto? Actual, IReadOnlyList<SolicitudPunto> Siguientes, int Total);
