using Comedor.Application.Abstracciones;
using Comedor.Domain.Accesos;

namespace Comedor.Application.Administracion;

/// <summary>
/// RF-26: captura posterior de la lista en papel. Solo aplica la restricción única, no las reglas de derecho.
/// </summary>
public class RegistrarContingencia(
    IEmpleadoRepositorio empleados,
    IPuntoRepositorio puntos,
    IAccesoRepositorio accesos,
    IReloj reloj)
{
    /// <exception cref="ArgumentException">Datos faltantes, matrícula no registrada o sin punto activo.</exception>
    /// <exception cref="ConsumoDuplicadoException">Ya existe el consumo.</exception>
    public async Task<Consumo> EjecutarAsync(string? matricula, DateOnly fechaServicio, string? tiempoComida,
        string? referenciaLista, string administrador)
    {
        var mat = (matricula ?? "").Trim().ToUpperInvariant();
        if (mat.Length == 0 || string.IsNullOrWhiteSpace(tiempoComida) || string.IsNullOrWhiteSpace(referenciaLista))
        {
            throw new ArgumentException("La matrícula, el tiempo de comida y la referencia de la lista son obligatorios");
        }

        if (await empleados.ObtenerAsync(mat) is null)
        {
            throw new ArgumentException(MotivoRechazoTexto.De(MotivoRechazo.NoRegistrado));
        }

        var punto = (await puntos.TodosAsync()).Where(p => p.Activo).OrderBy(p => p.Id, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new ArgumentException("No hay un punto de entrada activo");

        var consumo = Consumo.Crear(mat, fechaServicio, tiempoComida.Trim(), reloj.Ahora(), punto.Id,
            Canal.Contingencia, administrador, referenciaLista.Trim());
        await accesos.RegistrarConsumoAsync(consumo);
        return consumo;
    }
}
