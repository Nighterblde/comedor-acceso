namespace Comedor.Application.Usuarios;

/// <summary>Roles de los usuarios del sistema y del empleado.</summary>
public static class Roles
{
    public const string Chef = "Chef";
    public const string Administrador = "Administrador";
    public const string Empleado = "Empleado";

    /// <summary>Roles que inician sesión con usuario y contraseña propios (RF-24).</summary>
    public const string TodosDelSistema = Chef + "," + Administrador;
}

public sealed class UsuarioSistema
{
    public string Usuario { get; set; } = "";
    public string HashContrasena { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool Activo { get; set; } = true;
}
