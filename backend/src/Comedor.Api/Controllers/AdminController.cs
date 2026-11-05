using System.Globalization;
using Comedor.Application.Abstracciones;
using Comedor.Application.Administracion;
using Comedor.Application.Sincronizacion;
using Comedor.Application.Usuarios;
using Comedor.Domain.Puntos;
using Comedor.Domain.Reglas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

public record ReglaDto(string? Turno, string? TiempoComida, string? Inicio, string? Fin, bool PerteneceAJornadaAnterior);

public record PuntoDto(string? Id, string? Nombre, bool Activo);

public record UsuarioDto(string? Usuario, string? Rol, bool Activo, string? Contrasena);

public record ContingenciaDto(string? Matricula, string? FechaServicio, string? TiempoComida, string? ReferenciaLista);

public record ErrorAdmin(string Error);

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Administrador)]
public class AdminController(
    AdministrarReglas reglas,
    AdministrarPuntos puntos,
    AdministrarUsuarios usuarios,
    SincronizarPersonal sincronizar,
    IBitacoraSincronizacionRepositorio bitacora,
    RegistrarContingencia contingencia) : ControllerBase
{
    private const string FormatoHora = "HH:mm";

    [HttpGet("reglas")]
    public async Task<IActionResult> ListarReglas() =>
        Ok((await reglas.ListarAsync()).Select(r => new ReglaDto(r.Turno, r.TiempoComida,
            r.Inicio.ToString(FormatoHora, CultureInfo.InvariantCulture),
            r.Fin.ToString(FormatoHora, CultureInfo.InvariantCulture), r.PerteneceAJornadaAnterior)));

    [HttpPut("reglas")]
    public async Task<IActionResult> GuardarRegla([FromBody] ReglaDto? dto)
    {
        if (dto is null || !TimeOnly.TryParseExact(dto.Inicio, FormatoHora, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var inicio)
            || !TimeOnly.TryParseExact(dto.Fin, FormatoHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fin))
        {
            return Invalido("Indica el inicio y el fin con formato HH:mm");
        }

        try
        {
            await reglas.GuardarAsync(new ReglaDerecho((dto.Turno ?? "").Trim(), (dto.TiempoComida ?? "").Trim(),
                inicio, fin, dto.PerteneceAJornadaAnterior));
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return Invalido(ex.Message);
        }
    }

    [HttpDelete("reglas")]
    public async Task<IActionResult> EliminarRegla([FromQuery] string? turno, [FromQuery] string? tiempoComida) =>
        await reglas.EliminarAsync(turno ?? "", tiempoComida ?? "") ? NoContent() : NotFound();

    [HttpGet("puntos")]
    public async Task<IActionResult> ListarPuntos() =>
        Ok((await puntos.ListarAsync()).Select(p => new PuntoDto(p.Id, p.Nombre, p.Activo)));

    [HttpPut("puntos")]
    public async Task<IActionResult> GuardarPunto([FromBody] PuntoDto? dto)
    {
        try
        {
            await puntos.GuardarAsync(new PuntoEntrada(dto?.Id ?? "", dto?.Nombre ?? "", dto?.Activo ?? false));
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return Invalido(ex.Message);
        }
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> ListarUsuarios() => Ok(await usuarios.ListarAsync());

    [HttpPut("usuarios")]
    public async Task<IActionResult> GuardarUsuario([FromBody] UsuarioDto? dto)
    {
        try
        {
            await usuarios.GuardarAsync(dto?.Usuario ?? "", dto?.Rol ?? "", dto?.Activo ?? false, dto?.Contrasena);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return Invalido(ex.Message);
        }
    }

    [HttpPost("sincronizacion")]
    public async Task<ResultadoSincronizacion> Sincronizar() => await sincronizar.EjecutarAsync();

    [HttpGet("sincronizacion")]
    public async Task<IReadOnlyList<BitacoraSincronizacion>> UltimasSincronizaciones() =>
        await bitacora.UltimasAsync(20);

    [HttpPost("contingencia")]
    public async Task<IActionResult> Contingencia([FromBody] ContingenciaDto? dto)
    {
        if (!DateOnly.TryParseExact(dto?.FechaServicio, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fecha))
        {
            return Invalido("Indica la fecha de servicio con formato aaaa-MM-dd");
        }

        try
        {
            var autor = User.FindFirst("sub")?.Value ?? "";
            var c = await contingencia.EjecutarAsync(dto!.Matricula, fecha, dto.TiempoComida, dto.ReferenciaLista, autor);
            return Ok(new { c.Id, c.Matricula, FechaServicio = c.FechaServicio.ToString("yyyy-MM-dd"), c.TiempoComida });
        }
        catch (ConsumoDuplicadoException ex)
        {
            return Conflict(new ErrorAdmin(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Invalido(ex.Message);
        }
    }

    private BadRequestObjectResult Invalido(string texto) => BadRequest(new ErrorAdmin(texto));
}
