using ClosedXML.Excel;
using Comedor.Application.Reportes;

namespace Comedor.Infrastructure.Exportacion;

/// <summary>Genera los libros de Excel; el PDF vive en ExportadorPdf.</summary>
internal static class ExportadorExcel
{
    public static byte[] Conteo(IReadOnlyList<FilaConteo> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Conteo");
        Encabezado(hoja, "Fecha de servicio", "Tiempo de comida", "Turno", "Área", "Total");
        var fila = 2;
        foreach (var f in filas)
        {
            Texto(hoja.Cell(fila, 1), f.FechaServicio.ToString("yyyy-MM-dd"));
            Texto(hoja.Cell(fila, 2), f.TiempoComida);
            Texto(hoja.Cell(fila, 3), f.Turno);
            Texto(hoja.Cell(fila, 4), f.Area);
            hoja.Cell(fila, 5).Value = f.Total;
            fila++;
        }

        Texto(hoja.Cell(fila, 1), "Total");
        hoja.Cell(fila, 5).Value = filas.Sum(f => f.Total);
        hoja.Row(fila).Style.Font.Bold = true;
        return Bytes(libro, hoja);
    }

    public static byte[] Cotejo(IReadOnlyList<FilaCotejo> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Cotejo");
        Encabezado(hoja, "Fecha de servicio", "Tiempo de comida", "Servidas", "Sin servir");
        var fila = 2;
        foreach (var f in filas)
        {
            Texto(hoja.Cell(fila, 1), f.FechaServicio.ToString("yyyy-MM-dd"));
            Texto(hoja.Cell(fila, 2), f.TiempoComida);
            hoja.Cell(fila, 3).Value = f.Servidas;
            hoja.Cell(fila, 4).Value = f.SinServir;
            fila++;
        }

        Texto(hoja.Cell(fila, 1), "Total");
        hoja.Cell(fila, 3).Value = filas.Sum(f => f.Servidas);
        hoja.Cell(fila, 4).Value = filas.Sum(f => f.SinServir);
        hoja.Row(fila).Style.Font.Bold = true;
        return Bytes(libro, hoja);
    }

    public static byte[] Detalle(IReadOnlyList<FilaDetalle> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Detalle");
        Encabezado(hoja, "Fecha y hora", "Matrícula", "Nombre", "Tiempo de comida", "Punto", "Canal", "Resultado",
            "Motivo", "Servido");
        var fila = 2;
        foreach (var f in filas)
        {
            Texto(hoja.Cell(fila, 1), f.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"));
            Texto(hoja.Cell(fila, 2), f.Matricula);
            Texto(hoja.Cell(fila, 3), f.Nombre);
            Texto(hoja.Cell(fila, 4), f.TiempoComida);
            Texto(hoja.Cell(fila, 5), f.PuntoId);
            Texto(hoja.Cell(fila, 6), f.Canal.ToString());
            Texto(hoja.Cell(fila, 7), f.Resultado);
            Texto(hoja.Cell(fila, 8), f.Motivo ?? "");
            Texto(hoja.Cell(fila, 9), f.Servido ? "Sí" : "No");
            fila++;
        }

        return Bytes(libro, hoja);
    }

    /// <summary>
    /// Guarda el valor como texto literal: nunca como número (se perderían ceros a la izquierda) ni como fórmula
    /// (la matrícula de un rechazo "No registrado" la escribe el usuario).
    /// </summary>
    private static void Texto(IXLCell celda, string valor)
    {
        celda.Value = (XLCellValue)valor;
    }

    private static void Encabezado(IXLWorksheet hoja, params string[] titulos)
    {
        for (var i = 0; i < titulos.Length; i++)
        {
            Texto(hoja.Cell(1, i + 1), titulos[i]);
        }

        hoja.Row(1).Style.Font.Bold = true;
    }

    private static byte[] Bytes(XLWorkbook libro, IXLWorksheet hoja)
    {
        hoja.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }
}
