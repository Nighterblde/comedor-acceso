using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Application.Qr;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record RespuestaQr(string Token, string Url);

[ApiController]
[Route("api/puntos")]
public class PuntosController(
    EmitirTokenQr emitir,
    ListarSolicitudes listar,
    ConsultarFila fila,
    IConfiguration config) : ControllerBase
{
    [HttpGet("{id}/qr")]
    [Authorize(Roles = Roles.TodosDelSistema)]
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
            $"{baseUrl}/pase?t={Uri.EscapeDataString(token)}"));
    }

    [HttpGet("{id}/solicitudes")]
    [Authorize(Roles = Roles.TodosDelSistema)]
    public async Task<ActionResult<IReadOnlyList<SolicitudPunto>>> Solicitudes(string id, [FromQuery] DateTime? desde) =>
        Ok(await listar.EjecutarAsync(id, desde));

    [HttpGet("{id}/fila")]
    [Authorize(Roles = Roles.TodosDelSistema)]
    public async Task<ActionResult<FilaServicio>> Fila(string id) =>
        Ok(await fila.EjecutarAsync(id));
}
