using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Comedor.Application.Abstracciones;
using Microsoft.IdentityModel.Tokens;

namespace Comedor.Infrastructure.Seguridad;

public class EmisorJwt : IEmisorJwt
{
    private readonly SymmetricSecurityKey _clave;
    private readonly TimeProvider _proveedor;

    public EmisorJwt(string clave, TimeProvider? proveedor = null)
    {
        var bytes = string.IsNullOrEmpty(clave) ? [] : Encoding.UTF8.GetBytes(clave);
        if (bytes.Length < 32)
        {
            throw new ArgumentException("La clave JWT debe tener al menos 32 bytes (HS256).", nameof(clave));
        }

        _clave = new SymmetricSecurityKey(bytes);
        _proveedor = proveedor ?? TimeProvider.System;
    }

    public string Emitir(string sujeto, string rol, TimeSpan vigencia)
    {
        var ahora = _proveedor.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, sujeto),
                new Claim(ClaimTypes.Role, rol)
            ]),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora + vigencia,
            SigningCredentials = new SigningCredentials(_clave, SecurityAlgorithms.HmacSha256)
        };

        var manejador = new JwtSecurityTokenHandler();
        manejador.OutboundClaimTypeMap.Clear();
        return manejador.WriteToken(manejador.CreateToken(descriptor));
    }
}
