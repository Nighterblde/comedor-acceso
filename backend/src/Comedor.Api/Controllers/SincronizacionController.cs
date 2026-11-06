using Comedor.Application.Sincronizacion;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

/// <summary>Estado de la última sincronización, visible para el Administrador (RNF-05, D-22).</summary>
[ApiController]
[Route("api/sincronizacion")]
[Authorize(Roles = Roles.Administrador)]
public class SincronizacionController(ConsultarEstadoSincronizacion estado) : ControllerBase
{
    [HttpGet("estado")]
    public async Task<EstadoSincronizacion> Estado() => await estado.EjecutarAsync();
}
