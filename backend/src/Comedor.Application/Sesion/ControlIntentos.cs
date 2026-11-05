using Comedor.Application.Abstracciones;
using Microsoft.Extensions.Caching.Memory;

namespace Comedor.Application.Sesion;

/// <summary>
/// Bloquea un ID tras demasiados fallos de inicio de sesión. La caché solo almacena;
/// toda la aritmética de tiempo usa <see cref="IReloj"/>. Los llamadores deben pasar el id ya normalizado.
/// </summary>
public class ControlIntentos
{
    public const int MaximoFallos = 5;
    public static readonly TimeSpan VentanaFallos = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan RetencionCache = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly IReloj _reloj;
    private readonly object _candado = new();

    public ControlIntentos(IMemoryCache cache, IReloj reloj)
    {
        _cache = cache;
        _reloj = reloj;
    }

    private sealed class Estado
    {
        public List<DateTime> Fallos { get; } = [];
        public DateTime? BloqueadoHasta { get; set; }
    }

    public bool EstaBloqueado(string id)
    {
        lock (_candado)
        {
            return _cache.TryGetValue(Clave(id), out Estado? e) && e!.BloqueadoHasta > _reloj.Ahora();
        }
    }

    public void RegistrarFallo(string id)
    {
        lock (_candado)
        {
            var ahora = _reloj.Ahora();
            var e = _cache.GetOrCreate(Clave(id), c =>
            {
                c.SlidingExpiration = RetencionCache;
                return new Estado();
            })!;

            e.Fallos.RemoveAll(f => ahora - f > VentanaFallos);
            e.Fallos.Add(ahora);
            if (e.Fallos.Count >= MaximoFallos)
            {
                e.BloqueadoHasta = ahora + DuracionBloqueo;
                e.Fallos.Clear();
            }
        }
    }

    public void Limpiar(string id)
    {
        lock (_candado)
        {
            _cache.Remove(Clave(id));
        }
    }

    private static string Clave(string id) => "sesion-intentos:" + id;
}
