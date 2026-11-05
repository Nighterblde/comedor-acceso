using Comedor.Domain.Reglas;

namespace Comedor.Application.Abstracciones;

public interface IReglaRepositorio
{
    Task<IReadOnlyList<ReglaDerecho>> TodasAsync();

    /// <summary>Inserta o actualiza por (turno, tiempo de comida).</summary>
    Task GuardarAsync(ReglaDerecho r);

    Task EliminarAsync(string turno, string tiempoComida);
}
