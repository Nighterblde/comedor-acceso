using Comedor.Application.Abstracciones;

namespace Comedor.Application.Sincronizacion;

/// <summary>Estado de la sincronización con RR. HH. para el aviso de RNF-05 (D-22).</summary>
public record EstadoSincronizacion(bool UltimaFallida, DateTime? UltimaEjecucion, DateTime? UltimaExitosa);

public class ConsultarEstadoSincronizacion(IBitacoraSincronizacionRepositorio bitacora)
{
    public async Task<EstadoSincronizacion> EjecutarAsync()
    {
        var ultima = (await bitacora.UltimasAsync(1)).FirstOrDefault();
        var exitosa = await bitacora.UltimaExitosaAsync();
        return new EstadoSincronizacion(ultima is { Exito: false }, ultima?.Fecha, exitosa?.Fecha);
    }
}
