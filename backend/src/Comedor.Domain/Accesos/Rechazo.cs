namespace Comedor.Domain.Accesos;

public sealed class Rechazo
{
    private Rechazo()
    {
    }

    public Guid Id { get; private init; }
    public string Matricula { get; private init; } = "";
    public DateTime FechaHora { get; private init; }
    public DateOnly? FechaServicio { get; private init; }
    public string? TiempoComida { get; private init; }
    public string PuntoId { get; private init; } = "";
    public Canal Canal { get; private init; }
    public string? Encargado { get; private init; }
    public MotivoRechazo Motivo { get; private init; }

    public static Rechazo Crear(string matricula, DateTime fechaHora, DateOnly? fechaServicio, string? tiempoComida,
        string puntoId, Canal canal, string? encargado, MotivoRechazo motivo) => new()
    {
        Id = Guid.NewGuid(),
        Matricula = matricula,
        FechaHora = fechaHora,
        FechaServicio = fechaServicio,
        TiempoComida = tiempoComida,
        PuntoId = puntoId,
        Canal = canal,
        Encargado = encargado,
        Motivo = motivo
    };
}
