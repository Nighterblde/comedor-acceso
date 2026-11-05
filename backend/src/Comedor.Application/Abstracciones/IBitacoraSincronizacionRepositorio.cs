using Comedor.Application.Sincronizacion;

namespace Comedor.Application.Abstracciones;

public interface IBitacoraSincronizacionRepositorio
{
    Task RegistrarAsync(BitacoraSincronizacion fila);

    /// <summary>Las filas más recientes primero.</summary>
    Task<IReadOnlyList<BitacoraSincronizacion>> UltimasAsync(int cantidad);
}
