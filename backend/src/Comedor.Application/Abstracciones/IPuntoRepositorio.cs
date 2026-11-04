using Comedor.Domain.Puntos;

namespace Comedor.Application.Abstracciones;

public interface IPuntoRepositorio
{
    Task<PuntoEntrada?> ObtenerAsync(string id);

    Task<IReadOnlyList<PuntoEntrada>> TodosAsync();

    /// <summary>Inserta o actualiza por id.</summary>
    Task GuardarAsync(PuntoEntrada p);
}
