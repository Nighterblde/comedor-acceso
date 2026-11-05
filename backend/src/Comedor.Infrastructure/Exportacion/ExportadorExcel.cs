using ClosedXML.Excel;
using Comedor.Application.Reportes;

namespace Comedor.Infrastructure.Exportacion;

/// <summary>Genera los libros de Excel; el PDF vive en ExportadorPdf.</summary>
internal static class ExportadorExcel
{
    public static byte[] Cotejo(IReadOnlyList<FilaCotejo> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Cotejo");
        Encabezado(hoja, "Fecha de servicio", "Tiempo de comida", "Servidas", "Sin servir");
        var fila = 2;
        foreach (var f in filas)
        {
            hoja.Cell(fila, 1).Value = f.FechaServicio.ToString("yyyy-MM-dd");
            hoja.Cell(fila, 2).Value = f.TiempoComida;
            hoja.Cell(fila, 3).Value = f.Servidas;
            hoja.Cell(fila, 4).Value = f.SinServir;
            fila++;
        }

        hoja.Cell(fila, 1).Value = "Total";
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
            hoja.Cell(fila, 1).Value = f.FechaHora.ToString("yyyy-MM-dd HH:mm:ss");
            hoja.Cell(fila, 2).Value = f.Matricula;
            hoja.Cell(fila, 3).Value = f.Nombre;
            hoja.Cell(fila, 4).Value = f.TiempoComida;
            hoja.Cell(fila, 5).Value = f.PuntoId;
            hoja.Cell(fila, 6).Value = f.Canal.ToString();
            hoja.Cell(fila, 7).Value = f.Resultado;
            hoja.Cell(fila, 8).Value = f.Motivo ?? "";
            hoja.Cell(fila, 9).Value = f.Servido ? "Sí" : "No";
            fila++;
        }

        return Bytes(libro, hoja);
    }

    private static void Encabezado(IXLWorksheet hoja, params string[] titulos)
    {
        for (var i = 0; i < titulos.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = titulos[i];
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
