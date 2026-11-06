using ClosedXML.Excel;
using Comedor.Application.Sincronizacion;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure.Externos;
using Comedor.Infrastructure.Sincronizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comedor.Api.Controllers;

/// <summary>Importación del Excel de personal y su plantilla (RF-36, D-42).</summary>
[ApiController]
[Route("api/admin/personal")]
[Authorize(Roles = Roles.Administrador)]
public class PersonalController(ImportarPersonal importar) : ControllerBase
{
    private const long MaximoBytes = 5 * 1024 * 1024;
    private const string MensajeArchivo = "Sube un archivo .xlsx de hasta 5 MB";
    private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpPost("importar")]
    [RequestSizeLimit(MaximoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximoBytes + 1024 * 1024)]
    public async Task<IActionResult> Importar()
    {
        IFormFile? archivo;
        try
        {
            archivo = Request.HasFormContentType ? (await Request.ReadFormAsync()).Files.GetFile("archivo") : null;
        }
        catch (InvalidDataException)
        {
            archivo = null;
        }

        if (archivo is null
            || archivo.Length is 0 or > MaximoBytes
            || !string.Equals(Path.GetExtension(archivo.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorAdmin(MensajeArchivo));
        }

        // Un .xlsx es un ZIP: debe empezar con "PK". El nombre solo no basta (el archivo no es de confianza).
        await using var flujo = archivo.OpenReadStream();
        var firma = new byte[2];
        if (await flujo.ReadAsync(firma) != 2 || firma[0] != (byte)'P' || firma[1] != (byte)'K')
        {
            return BadRequest(new ErrorAdmin(MensajeArchivo));
        }

        flujo.Position = 0;
        return Ok(await importar.EjecutarAsync(flujo));
    }

    [HttpGet("plantilla.xlsx")]
    public IActionResult Plantilla()
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Personal");
        for (var i = 0; i < FuenteRRHHExcel.Columnas.Count; i++)
        {
            hoja.Cell(1, i + 1).Value = FuenteRRHHExcel.Columnas[i];
        }

        hoja.Range(1, 1, 1, FuenteRRHHExcel.Columnas.Count).Style.Font.Bold = true;
        hoja.Column(1).Style.NumberFormat.Format = "@"; // matrícula como texto: conserva ceros a la izquierda
        hoja.Cell(2, 1).Value = "A0001";
        hoja.Cell(2, 2).Value = "Ana Pérez López";
        hoja.Cell(2, 3).Value = "Producción";
        hoja.Cell(2, 4).Value = "Matutino";
        hoja.Cell(2, 5).Value = "L,M,X,J,V";
        hoja.Cell(2, 6).Value = new DateTime(2026, 1, 1);
        hoja.Cell(2, 7).Value = new DateTime(2026, 12, 31);
        hoja.Range(2, 6, 2, 7).Style.DateFormat.Format = "yyyy-mm-dd";
        hoja.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return File(ms.ToArray(), TipoXlsx, "plantilla-personal.xlsx");
    }
}
