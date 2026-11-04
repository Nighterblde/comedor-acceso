using Comedor.Application.Abstracciones;

namespace Comedor.Application.Qr;

public class ValidadorTokenQr(
    IPuntoRepositorio puntos, IFirmaToken firma, IReloj reloj, OpcionesQr opciones)
{
    /// <summary>Devuelve el PuntoId si el token es auténtico, vigente y de un punto activo; si no, null.</summary>
    public async Task<string?> ValidarAsync(string token)
    {
        var t = firma.Verificar(token);
        if (t is null)
        {
            return null;
        }

        var antiguedad = reloj.Ahora() - t.EmitidoEn;
        if (antiguedad < TimeSpan.Zero || antiguedad > TimeSpan.FromSeconds(opciones.SegundosValidez))
        {
            return null;
        }

        var punto = await puntos.ObtenerAsync(t.PuntoId);
        return punto is { Activo: true } ? punto.Id : null;
    }
}
