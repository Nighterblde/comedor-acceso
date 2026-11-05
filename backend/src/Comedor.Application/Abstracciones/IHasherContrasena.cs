using Comedor.Application.Usuarios;

namespace Comedor.Application.Abstracciones;

public interface IHasherContrasena
{
    string Hash(UsuarioSistema u, string contrasena);

    bool Verificar(UsuarioSistema u, string hash, string contrasena);
}
