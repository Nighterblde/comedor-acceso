using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Comedor.Application.Abstracciones;
using Comedor.Application.Qr;

namespace Comedor.Infrastructure.Seguridad;

public class FirmaTokenHmac : IFirmaToken
{
    private readonly byte[] _clave;

    public FirmaTokenHmac(string clave)
    {
        if (string.IsNullOrEmpty(clave))
        {
            throw new ArgumentException("La clave de firma no puede estar vacía.", nameof(clave));
        }

        _clave = Encoding.UTF8.GetBytes(clave);
    }

    public string Firmar(TokenQr t)
    {
        var parte1 = ABase64Url(Encoding.UTF8.GetBytes($"{t.PuntoId}|{t.EmitidoEn.Ticks}"));
        var mac = HMACSHA256.HashData(_clave, Encoding.UTF8.GetBytes(parte1));
        return parte1 + "." + ABase64Url(mac);
    }

    public TokenQr? Verificar(string token)
    {
        try
        {
            var partes = token.Split('.');
            if (partes.Length != 2)
            {
                return null;
            }

            var macRecibido = DeBase64Url(partes[1]);
            var macEsperado = HMACSHA256.HashData(_clave, Encoding.UTF8.GetBytes(partes[0]));
            if (macRecibido is null || !CryptographicOperations.FixedTimeEquals(macRecibido, macEsperado))
            {
                return null;
            }

            var bytes = DeBase64Url(partes[0]);
            if (bytes is null)
            {
                return null;
            }

            var contenido = Encoding.UTF8.GetString(bytes);
            var corte = contenido.LastIndexOf('|');
            if (corte <= 0 ||
                !long.TryParse(contenido[(corte + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                ticks > DateTime.MaxValue.Ticks)
            {
                return null;
            }

            return new TokenQr(contenido[..corte], new DateTime(ticks));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ABase64Url(byte[] datos) =>
        Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? DeBase64Url(string texto)
    {
        var b64 = texto.Replace('-', '+').Replace('_', '/');
        b64 = b64.PadRight(b64.Length + ((4 - (b64.Length % 4)) % 4), '=');
        try
        {
            return Convert.FromBase64String(b64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
