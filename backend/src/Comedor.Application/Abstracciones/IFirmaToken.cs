using Comedor.Application.Qr;

namespace Comedor.Application.Abstracciones;

public interface IFirmaToken
{
    string Firmar(TokenQr t);

    /// <summary>Devuelve null si el formato o la firma son inválidos.</summary>
    TokenQr? Verificar(string token);
}
