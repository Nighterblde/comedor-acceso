using System.Globalization;
using Comedor.Application.Abstracciones;
using Comedor.Application.Reportes;
using Comedor.Domain.Accesos;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = Roles.Administrador)]
public class ReportesController(GenerarReporte reportes, IExportador exportador) : ControllerBase
{
    private const string TipoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("conteo")]
    public async Task<IActionResult> Conteo([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        return Ok(await reportes.ConteoAsync(d, h));
    }

    [HttpGet("cotejo")]
    public async Task<IActionResult> Cotejo([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        return Ok(await reportes.CotejoFacturaAsync(d, h));
    }

    [HttpGet("detalle")]
    public async Task<IActionResult> Detalle([FromQuery] string? desde, [FromQuery] string? hasta,
        [FromQuery] string? matricula, [FromQuery] string? puntoId, [FromQuery] Canal? canal,
        [FromQuery] MotivoRechazo? motivo)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        return Ok(await reportes.DetalleAsync(d, h, matricula, puntoId, canal, motivo));
    }

    [HttpGet("cotejo.xlsx")]
    public async Task<IActionResult> CotejoExcel([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        var filas = await reportes.CotejoFacturaAsync(d, h);
        return File(exportador.CotejoExcel(filas, d, h), TipoExcel, $"cotejo-{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("cotejo.pdf")]
    public async Task<IActionResult> CotejoPdf([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        var filas = await reportes.CotejoFacturaAsync(d, h);
        return File(exportador.CotejoPdf(filas, d, h), "application/pdf", $"cotejo-{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.pdf");
    }

    [HttpGet("conteo.xlsx")]
    public async Task<IActionResult> ConteoExcel([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        var filas = await reportes.ConteoAsync(d, h);
        return File(exportador.ConteoExcel(filas, d, h), TipoExcel, $"conteo-{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("conteo.pdf")]
    public async Task<IActionResult> ConteoPdf([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        var filas = await reportes.ConteoAsync(d, h);
        return File(exportador.ConteoPdf(filas, d, h), "application/pdf", $"conteo-{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.pdf");
    }

    [HttpGet("detalle.xlsx")]
    public async Task<IActionResult> DetalleExcel([FromQuery] string? desde, [FromQuery] string? hasta,
        [FromQuery] string? matricula, [FromQuery] string? puntoId, [FromQuery] Canal? canal,
        [FromQuery] MotivoRechazo? motivo)
    {
        if (!Rango(desde, hasta, out var d, out var h)) return RangoInvalido();
        var filas = await reportes.DetalleAsync(d, h, matricula, puntoId, canal, motivo);
        return File(exportador.DetalleExcel(filas), TipoExcel, $"detalle-{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.xlsx");
    }

    private static bool Rango(string? desde, string? hasta, out DateOnly d, out DateOnly h)
    {
        d = h = default;
        return DateOnly.TryParseExact(desde, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
            && DateOnly.TryParseExact(hasta, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out h)
            && d <= h;
    }

    private BadRequestObjectResult RangoInvalido() =>
        BadRequest(new ProblemDetails
        {
            Status = 400,
            Title = "Indica un rango válido: desde y hasta con formato aaaa-MM-dd, y desde no mayor que hasta.",
        });
}
