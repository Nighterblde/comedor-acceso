using Comedor.Application.Abstracciones;
using Comedor.Domain.Reglas;

namespace Comedor.Application.Administracion;

/// <summary>RF-05 y RF-28: configuración de reglas de derecho sin horarios traslapados dentro de un turno.</summary>
public class AdministrarReglas(IReglaRepositorio reglas)
{
    public Task<IReadOnlyList<ReglaDerecho>> ListarAsync() => reglas.TodasAsync();

    /// <exception cref="ArgumentException">Horario inválido o traslapado con otra regla del mismo turno.</exception>
    public async Task GuardarAsync(ReglaDerecho regla)
    {
        if (string.IsNullOrWhiteSpace(regla.Turno) || string.IsNullOrWhiteSpace(regla.TiempoComida))
        {
            throw new ArgumentException("El turno y el tiempo de comida son obligatorios");
        }

        if (regla.Inicio == regla.Fin)
        {
            throw new ArgumentException("El inicio y el fin no pueden ser iguales");
        }

        // Guardar es un upsert por (turno, tiempo de comida): la regla que se reemplaza no cuenta.
        var otras = (await reglas.TodasAsync())
            .Where(o => !(o.Turno == regla.Turno && o.TiempoComida == regla.TiempoComida));
        var choque = otras.FirstOrDefault(regla.SeTraslapaCon);
        if (choque is not null)
        {
            throw new ArgumentException(
                $"Se traslapa con {choque.TiempoComida} de {choque.Inicio:HH:mm} a {choque.Fin:HH:mm}");
        }

        await reglas.GuardarAsync(regla);
    }

    /// <returns>false si la regla no existía.</returns>
    public async Task<bool> EliminarAsync(string turno, string tiempoComida)
    {
        var existe = (await reglas.TodasAsync()).Any(r => r.Turno == turno && r.TiempoComida == tiempoComida);
        if (existe)
        {
            await reglas.EliminarAsync(turno, tiempoComida);
        }

        return existe;
    }
}
