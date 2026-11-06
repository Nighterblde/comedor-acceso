using System.Net;
using System.Net.Sockets;

namespace Comedor.Api.Configuracion;

/// <summary>
/// Restringe la API a la red de la empresa (D-38, RF-37). Una petición con encabezados de túnel o proxy es externa
/// aunque el socket sea de loopback; los encabezados no se usan para nada más.
/// </summary>
public static class RedLocal
{
    public const string MensajeFuera = "Disponible solo desde la red de la empresa";

    public static readonly string[] RutasPublicas =
        ["/api/empleados/sesion", "/api/accesos/celular", "/api/accesos/mi-turno", "/api/salud"];

    public static bool EsRedLocal(IPAddress? ip, IHeaderDictionary encabezados)
    {
        if (encabezados.ContainsKey("CF-Connecting-IP") || encabezados.ContainsKey("X-Forwarded-For"))
        {
            return false;
        }

        if (ip is null)
        {
            return true;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        var b = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254);
        }

        return ip.AddressFamily == AddressFamily.InterNetworkV6
            && ((b[0] & 0xFE) == 0xFC || (b[0] == 0xFE && (b[1] & 0xC0) == 0x80));
    }

    private static bool EsRutaPublica(PathString ruta)
    {
        var texto = ruta.Value?.TrimEnd('/') ?? string.Empty;
        return RutasPublicas.Any(p => string.Equals(p, texto, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Responde 403 a cualquier /api/* no público cuando la petición no viene de la red de la empresa.</summary>
    public static IApplicationBuilder UseSoloRedLocal(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api")
                && !EsRutaPublica(contexto.Request.Path)
                && !EsRedLocal(contexto.Connection.RemoteIpAddress, contexto.Request.Headers))
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                await contexto.Response.WriteAsJsonAsync(new { error = MensajeFuera });
                return;
            }

            await siguiente();
        });
}
