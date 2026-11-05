using Comedor.Application.Abstracciones;

namespace Comedor.Infrastructure.Externos;

/// <summary>Autenticador de desarrollo: pares id y contraseña en configuración.</summary>
public class AutenticadorEmpresaSimulado : IAutenticadorEmpresa
{
    private readonly Dictionary<string, string> _usuarios;

    public AutenticadorEmpresaSimulado(IReadOnlyDictionary<string, string> usuarios)
    {
        _usuarios = usuarios.ToDictionary(u => Normalizar(u.Key), u => u.Value);
    }

    public Task<bool> ValidarAsync(string id, string contrasena) =>
        Task.FromResult(_usuarios.TryGetValue(Normalizar(id), out var esperada) &&
                        string.Equals(esperada, contrasena, StringComparison.Ordinal));

    private static string Normalizar(string id) => id.Trim().ToUpperInvariant();
}
