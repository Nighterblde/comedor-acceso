using Comedor.Application.Abstracciones;

namespace Comedor.Application.Qr;

public class EmitirTokenQr(IPuntoRepositorio puntos, IFirmaToken firma, IReloj reloj)
{
    public async Task<string> EjecutarAsync(string puntoId)
    {
        var punto = await puntos.ObtenerAsync(puntoId);
        if (punto is null || !punto.Activo)
        {
            throw new InvalidOperationException("El punto de entrada no existe o está inactivo.");
        }

        return firma.Firmar(new TokenQr(puntoId, reloj.Ahora()));
    }
}
