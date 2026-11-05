using Comedor.Application.Accesos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record SolicitudAccesoCelular(string? TokenQr);

public record SolicitudAccesoManual(string? Matricula, string? PuntoId);

[ApiController]
[Route("api/accesos")]
public class AccesosController(ValidarAcceso validar) : ControllerBase
{
    [HttpPost("celular")]
    [Authorize(Roles = "Empleado")]
    public async Task<IActionResult> Celular([FromBody] SolicitudAccesoCelular? solicitud)
    {
        var matricula = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(solicitud?.TokenQr))
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Falta el código QR." });
        }

        return Ok(await validar.PorCelularAsync(matricula, solicitud.TokenQr));
    }

    [HttpPost("manual")]
    [Authorize(Roles = "Punto,Encargado")]
    public async Task<IActionResult> Manual([FromBody] SolicitudAccesoManual? solicitud)
    {
        var encargado = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(encargado))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(solicitud?.Matricula) || string.IsNullOrWhiteSpace(solicitud.PuntoId))
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Faltan la matrícula o el punto." });
        }

        return Ok(await validar.ManualAsync(solicitud.Matricula, solicitud.PuntoId, encargado));
    }
}
