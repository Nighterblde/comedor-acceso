using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;

namespace Comedor.Api.Tareas;

/// <summary>Ejecuta la sincronización de personal cada noche a la hora configurada.</summary>
public class SincronizacionNocturna(
    IServiceScopeFactory scopes,
    IReloj reloj,
    OpcionesSincronizacion opciones,
    ILogger<SincronizacionNocturna> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TiempoHastaSiguiente(reloj.Ahora(), opciones.Hora), stoppingToken);
                await using var scope = scopes.CreateAsyncScope();
                var resultado = await scope.ServiceProvider.GetRequiredService<SincronizarPersonal>().EjecutarAsync();
                logger.LogInformation("Sincronización de personal: éxito={Exito}, altas={Altas}, cambios={Cambios}, bajas={Bajas}, omitidos={Omitidos}",
                    resultado.Exito, resultado.Altas, resultado.Cambios, resultado.Bajas, resultado.Omitidos);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló la sincronización nocturna de personal");
            }
        }
    }

    /// <summary>Tiempo hasta la próxima ocurrencia de la hora; si ya es esa hora, espera 24 h.</summary>
    internal static TimeSpan TiempoHastaSiguiente(DateTime ahora, TimeOnly hora)
    {
        var siguiente = ahora.Date + hora.ToTimeSpan();
        if (siguiente <= ahora)
        {
            siguiente = siguiente.AddDays(1);
        }

        return siguiente - ahora;
    }
}
