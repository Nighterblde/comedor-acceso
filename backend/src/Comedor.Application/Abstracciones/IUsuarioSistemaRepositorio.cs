using Comedor.Application.Usuarios;

namespace Comedor.Application.Abstracciones;

public interface IUsuarioSistemaRepositorio
{
    Task<UsuarioSistema?> ObtenerAsync(string usuario);

    Task<IReadOnlyList<UsuarioSistema>> TodosAsync();

    /// <summary>Inserta o actualiza por usuario.</summary>
    Task GuardarAsync(UsuarioSistema u);

    Task<bool> ExisteAlgunoAsync();
}
