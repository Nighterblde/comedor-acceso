using Comedor.Application.Sesion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record SolicitudSesionEmpleado(string? Id, string? Contrasena);

public record RespuestaSesionEmpleado(string Token);

public record RespuestaErrorSesion(string Error);

[ApiController]
[Route("api/empleados")]
public class EmpleadosSesionController(IniciarSesionEmpleado iniciarSesion) : ControllerBase
{
    [HttpPost("sesion")]
    [AllowAnonymous]
    public async Task<IActionResult> Iniciar([FromBody] SolicitudSesionEmpleado? solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud?.Id) || string.IsNullOrEmpty(solicitud.Contrasena))
        {
            return Unauthorized(new RespuestaErrorSesion(nameof(ErrorSesion.CredencialesIncorrectas)));
        }

        var r = await iniciarSesion.EjecutarAsync(solicitud.Id, solicitud.Contrasena);
        return r.Exito
            ? Ok(new RespuestaSesionEmpleado(r.Token!))
            : Unauthorized(new RespuestaErrorSesion((r.Error ?? ErrorSesion.CredencialesIncorrectas).ToString()));
    }
}
