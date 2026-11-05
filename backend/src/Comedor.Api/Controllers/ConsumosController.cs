using Comedor.Application.Accesos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

[ApiController]
[Route("api/consumos")]
public class ConsumosController(MarcarServido marcarServido) : ControllerBase
{
    [HttpPost("{id:guid}/servido")]
    [Authorize(Roles = "Punto,Encargado")]
    public async Task<IActionResult> Servido(Guid id)
    {
        var usuario = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(usuario))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await marcarServido.EjecutarAsync(id, usuario));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "El consumo no existe." });
        }
    }
}
