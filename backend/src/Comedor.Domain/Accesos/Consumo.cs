namespace Comedor.Domain.Accesos;

public sealed class Consumo
{
    private Consumo()
    {
    }

    public Guid Id { get; private init; }
    public string Matricula { get; private init; } = "";
    public DateOnly FechaServicio { get; private init; }
    public string TiempoComida { get; private init; } = "";
    public DateTime FechaHora { get; private init; }
    public string PuntoId { get; private init; } = "";
    public Canal Canal { get; private init; }
    public string? Encargado { get; private init; }
    public string? Referencia { get; private init; }

    public static Consumo Crear(string matricula, DateOnly fechaServicio, string tiempoComida, DateTime fechaHora,
        string puntoId, Canal canal, string? encargado, string? referencia = null) => new()
    {
        Id = Guid.NewGuid(),
        Matricula = matricula,
        FechaServicio = fechaServicio,
        TiempoComida = tiempoComida,
        FechaHora = fechaHora,
        PuntoId = puntoId,
        Canal = canal,
        Encargado = encargado,
        Referencia = referencia
    };
}
