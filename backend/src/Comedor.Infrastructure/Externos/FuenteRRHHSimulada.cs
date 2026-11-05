using System.Text.Json;
using System.Text.Json.Serialization;
using Comedor.Application.Abstracciones;

namespace Comedor.Infrastructure.Externos;

/// <summary>Fuente de RR. HH. de desarrollo: lee un archivo JSON. Solo lectura (RF-03).</summary>
public class FuenteRRHHSimulada(string rutaArchivo) : IFuentePersonalRRHH
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync()
    {
        await using var flujo = File.OpenRead(rutaArchivo);
        var filas = await JsonSerializer.DeserializeAsync<List<FilaJson>>(flujo, Opciones) ?? [];
        return filas
            .Select(f => new EmpleadoFuente(f.Matricula, f.Nombre, f.Area, f.Turno, f.DiasLaborales.ToHashSet(),
                f.InicioContrato, f.FinContrato, f.FotoUrl))
            .ToList();
    }

    private sealed record FilaJson(
        string Matricula,
        string Nombre,
        string Area,
        string Turno,
        List<DayOfWeek> DiasLaborales,
        DateOnly InicioContrato,
        DateOnly FinContrato,
        string? FotoUrl);
}
