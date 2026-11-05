using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Application.Qr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record RespuestaQr(string Token, string Url, DateTime ExpiraEn);

[ApiController]
[Route("api/puntos")]
[Authorize(Roles = "Punto,Encargado")]
public class PuntosController(
    EmitirTokenQr emitir,
    ListarSolicitudes listar,
    OpcionesQr opciones,
    IReloj reloj,
    IConfiguration config) : ControllerBase
{
    [HttpGet("{id}/qr")]
    public async Task<IActionResult> Qr(string id)
    {
        string token;
        try
        {
            token = await emitir.EjecutarAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "El punto de entrada no existe o está inactivo.",
            });
        }

        var baseUrl = (config["App:UrlPublica"] ?? "").TrimEnd('/');
        return Ok(new RespuestaQr(
            token,
            $"{baseUrl}/pase?t={Uri.EscapeDataString(token)}",
            reloj.Ahora().AddSeconds(opciones.SegundosValidez)));
    }

    [HttpGet("{id}/solicitudes")]
    public async Task<ActionResult<IReadOnlyList<SolicitudPunto>>> Solicitudes(string id, [FromQuery] DateTime? desde) =>
        Ok(await listar.EjecutarAsync(id, desde));
}
