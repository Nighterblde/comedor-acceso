namespace Comedor.Application.Abstracciones;

public interface IAutenticadorEmpresa
{
    /// <summary>Valida el ID y la contraseña de la empresa. Lanza excepción si el servicio no responde.</summary>
    Task<bool> ValidarAsync(string id, string contrasena);
}
