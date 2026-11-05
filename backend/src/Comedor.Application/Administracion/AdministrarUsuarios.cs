using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;

namespace Comedor.Application.Administracion;

/// <summary>Usuario sin contraseña ni hash: es lo único que sale de la administración.</summary>
public record UsuarioVista(string Usuario, string Rol, bool Activo);

/// <summary>RF-25: alta, edición y baja (activo = false) de usuarios del sistema.</summary>
public class AdministrarUsuarios(IUsuarioSistemaRepositorio usuarios, IHasherContrasena hasher)
{
    private static readonly string[] RolesValidos =
        [Roles.Punto, Roles.Encargado, Roles.Supervisor, Roles.Administrador];

    public async Task<IReadOnlyList<UsuarioVista>> ListarAsync() =>
        (await usuarios.TodosAsync()).Select(u => new UsuarioVista(u.Usuario, u.Rol, u.Activo)).ToList();

    /// <param name="contrasena">Obligatoria al crear; al editar, vacía conserva la actual.</param>
    public async Task GuardarAsync(string usuario, string rol, bool activo, string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException("El usuario es obligatorio");
        }

        if (!RolesValidos.Contains(rol))
        {
            throw new ArgumentException("El rol no es válido");
        }

        usuario = usuario.Trim();
        var existente = await usuarios.ObtenerAsync(usuario);
        if (existente is null && string.IsNullOrEmpty(contrasena))
        {
            throw new ArgumentException("La contraseña es obligatoria al crear un usuario");
        }

        var u = new UsuarioSistema
        {
            Usuario = usuario,
            Rol = rol,
            Activo = activo,
            HashContrasena = existente?.HashContrasena ?? "",
        };
        if (!string.IsNullOrEmpty(contrasena))
        {
            u.HashContrasena = hasher.Hash(u, contrasena);
        }

        await usuarios.GuardarAsync(u);
    }
}
