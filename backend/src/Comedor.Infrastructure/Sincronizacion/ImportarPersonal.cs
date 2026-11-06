using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;
using Comedor.Infrastructure.Externos;

namespace Comedor.Infrastructure.Sincronizacion;

/// <summary>
/// Importa el Excel de personal (RF-36, D-42): lo guarda en <c>Fuente:RutaExcel</c> y sincroniza con
/// <see cref="FuenteRRHHExcel"/> usando <see cref="SincronizarPersonal"/>, así que D-28 aplica igual.
/// </summary>
/// <remarks>
/// El archivo se escribe primero en un temporal junto al destino y solo reemplaza al anterior si la
/// sincronización tuvo éxito: un archivo ilegible no destruye el último válido (D-49).
/// </remarks>
public class ImportarPersonal(
    string ruta,
    IEmpleadoRepositorio empleados,
    IBitacoraSincronizacionRepositorio bitacora,
    IReloj reloj,
    OpcionesSincronizacion opciones)
{
    private static readonly SemaphoreSlim Candado = new(1, 1);

    public async Task<ResultadoSincronizacion> EjecutarAsync(Stream excel)
    {
        await Candado.WaitAsync();
        string? temporal = null;
        try
        {
            var carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta))!;
            Directory.CreateDirectory(carpeta);
            temporal = Path.Combine(carpeta, Guid.NewGuid().ToString("N") + ".importando.xlsx");
            await using (var destino = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write))
            {
                await excel.CopyToAsync(destino);
            }

            var sincronizar = new SincronizarPersonal(new FuenteRRHHExcel(temporal), empleados, bitacora, reloj, opciones);
            var resultado = await sincronizar.EjecutarAsync();
            if (resultado.Exito)
            {
                File.Move(temporal, ruta, true);
                temporal = null;
            }

            return resultado;
        }
        finally
        {
            if (temporal is not null)
            {
                File.Delete(temporal);
            }

            Candado.Release();
        }
    }
}
