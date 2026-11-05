using System.Security.Claims;
using Comedor.Application.Sesion;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record SolicitudLogin(string? Usuario, string? Contrasena);

public record RespuestaLogin(string Token, string Rol, DateTime ExpiraEn);

public record RespuestaYo(string? Usuario, string? Rol);

[ApiController]
[Route("api/auth")]
public class AuthController(IniciarSesionSistema iniciarSesion) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] SolicitudLogin? solicitud)
    {
        var r = await iniciarSesion.EjecutarAsync(solicitud?.Usuario, solicitud?.Contrasena);
        if (!r.Exito)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales incorrectas",
            });
        }

        return Ok(new RespuestaLogin(r.Token!, r.Rol!, r.ExpiraEn!.Value));
    }

    [HttpGet("yo")]
    [Authorize(Roles = Roles.TodosDelSistema)]
    public IActionResult Yo() =>
        Ok(new RespuestaYo(User.FindFirst("sub")?.Value, User.FindFirst(ClaimTypes.Role)?.Value));
}
