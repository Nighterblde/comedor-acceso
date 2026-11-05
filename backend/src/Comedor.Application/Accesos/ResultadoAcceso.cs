namespace Comedor.Application.Accesos;

public enum EstadoAcceso
{
    Autorizado,
    Rechazado,
    EscanearDeNuevo
}

public record ResultadoAcceso(
    EstadoAcceso Estado,
    string? Motivo,
    string? Matricula,
    string? Nombre,
    string? Turno,
    string? TiempoComida,
    string? FotoUrl,
    DateTime Hora);
