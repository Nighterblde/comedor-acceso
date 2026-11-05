using Comedor.Application.Abstracciones;
using Comedor.Domain.Accesos;

namespace Comedor.Application.Accesos;

public record ResultadoServido(bool YaServido, DateTime ServidoEn);

public class MarcarServido(IAccesoRepositorio accesos, IReloj reloj)
{
    /// <exception cref="KeyNotFoundException">El consumo no existe.</exception>
    public async Task<ResultadoServido> EjecutarAsync(Guid consumoId, string usuario)
    {
        if (await accesos.ObtenerConsumoAsync(consumoId) is null)
        {
            throw new KeyNotFoundException("El consumo no existe.");
        }

        var ahora = reloj.Ahora();
        try
        {
            await accesos.RegistrarEntregaAsync(Entrega.Crear(consumoId, ahora, usuario));
        }
        catch (EntregaDuplicadaException)
        {
            return new ResultadoServido(true, ahora);
        }

        return new ResultadoServido(false, ahora);
    }
}
