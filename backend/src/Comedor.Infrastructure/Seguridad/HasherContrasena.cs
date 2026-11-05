using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace Comedor.Infrastructure.Seguridad;

/// <summary>Hash de contraseñas con <see cref="PasswordHasher{TUser}"/> (RNF-06).</summary>
public class HasherContrasena : IHasherContrasena
{
    private readonly PasswordHasher<UsuarioSistema> _hasher = new();

    public string Hash(UsuarioSistema u, string contrasena) => _hasher.HashPassword(u, contrasena);

    public bool Verificar(UsuarioSistema u, string hash, string contrasena)
    {
        try
        {
            var r = _hasher.VerifyHashedPassword(u, hash, contrasena);
            return r is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
