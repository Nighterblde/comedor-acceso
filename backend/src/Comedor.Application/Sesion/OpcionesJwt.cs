namespace Comedor.Application.Sesion;

public class OpcionesJwt
{
    /// <summary>Vigencia de la sesión del chef (sin caducidad práctica, revocable por petición; D-34).</summary>
    public int DiasChef { get; set; } = 3650;

    public int HorasAdministrador { get; set; } = 2;
}
