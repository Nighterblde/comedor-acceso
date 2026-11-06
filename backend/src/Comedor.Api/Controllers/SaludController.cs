using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

[ApiController]
[Route("api/salud")]
[AllowAnonymous]
public class SaludController : ControllerBase
{
    private static readonly string Version =
        typeof(SaludController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(SaludController).Assembly.GetName().Version?.ToString()
        ?? "desconocida";

    [HttpGet]
    public IActionResult Obtener() => Ok(new { estado = "ok", version = Version });
}
