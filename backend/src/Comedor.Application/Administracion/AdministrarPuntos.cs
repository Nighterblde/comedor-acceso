using Comedor.Application.Abstracciones;
using Comedor.Domain.Puntos;

namespace Comedor.Application.Administracion;

/// <summary>RF-25: alta, edición y baja (activo = false) de puntos de entrada.</summary>
public class AdministrarPuntos(IPuntoRepositorio puntos)
{
    public Task<IReadOnlyList<PuntoEntrada>> ListarAsync() => puntos.TodosAsync();

    public async Task GuardarAsync(PuntoEntrada punto)
    {
        if (string.IsNullOrWhiteSpace(punto.Id) || string.IsNullOrWhiteSpace(punto.Nombre))
        {
            throw new ArgumentException("El id y el nombre del punto son obligatorios");
        }

        await puntos.GuardarAsync(punto with { Id = punto.Id.Trim(), Nombre = punto.Nombre.Trim() });
    }
}
