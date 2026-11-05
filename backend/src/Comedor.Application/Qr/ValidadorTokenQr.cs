using Comedor.Application.Abstracciones;

namespace Comedor.Application.Qr;

public class ValidadorTokenQr(IPuntoRepositorio puntos, IFirmaToken firma)
{
    /// <summary>Devuelve el PuntoId si el token es auténtico y de un punto activo, sin importar su antigüedad; si no, null.</summary>
    public async Task<string?> ValidarAsync(string token)
    {
        var t = firma.Verificar(token);
        if (t is null)
        {
            return null;
        }

        var punto = await puntos.ObtenerAsync(t.PuntoId);
        return punto is { Activo: true } ? punto.Id : null;
    }
}
