namespace Comedor.Domain.Accesos;

public sealed class Entrega
{
    private Entrega()
    {
    }

    public Guid Id { get; private init; }
    public Guid ConsumoId { get; private init; }
    public DateTime FechaHora { get; private init; }
    public string Usuario { get; private init; } = "";

    public static Entrega Crear(Guid consumoId, DateTime fechaHora, string usuario) => new()
    {
        Id = Guid.NewGuid(),
        ConsumoId = consumoId,
        FechaHora = fechaHora,
        Usuario = usuario
    };
}
