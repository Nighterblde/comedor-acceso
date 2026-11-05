using Comedor.Application.Accesos;
using Comedor.Domain.Accesos;

namespace Comedor.Application.Abstracciones;

/// <summary>Registro de solo inserción: sin métodos para actualizar ni borrar.</summary>
public interface IAccesoRepositorio
{
    Task<bool> YaConsumioAsync(string matricula, DateOnly fechaServicio, string tiempoComida);

    /// <exception cref="ConsumoDuplicadoException">Ya existe el consumo para esa clave.</exception>
    Task RegistrarConsumoAsync(Consumo c);

    Task RegistrarRechazoAsync(Rechazo r);

    /// <exception cref="EntregaDuplicadaException">El consumo ya tiene entrega.</exception>
    Task RegistrarEntregaAsync(Entrega e);

    Task<Consumo?> ObtenerConsumoAsync(Guid id);

    Task<bool> EstaServidoAsync(Guid consumoId);

    Task<IReadOnlyList<SolicitudPunto>> SolicitudesAsync(string puntoId, DateTime desde);
}
