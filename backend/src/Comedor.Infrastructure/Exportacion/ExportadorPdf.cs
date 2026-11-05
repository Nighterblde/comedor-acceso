using Comedor.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Comedor.Infrastructure.Exportacion;

internal static class ExportadorPdf
{
    static ExportadorPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Cotejo(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta) =>
        Document.Create(doc => doc.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(36);
            pagina.DefaultTextStyle(t => t.FontSize(11));
            pagina.Header().Text($"Cotejo de factura del {desde:yyyy-MM-dd} al {hasta:yyyy-MM-dd}")
                .FontSize(16).SemiBold();
            pagina.Content().PaddingTop(12).Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn();
                    c.RelativeColumn();
                });
                tabla.Header(h =>
                {
                    h.Cell().Element(Celda).Text("Fecha de servicio").SemiBold();
                    h.Cell().Element(Celda).Text("Tiempo de comida").SemiBold();
                    h.Cell().Element(Celda).AlignRight().Text("Servidas").SemiBold();
                    h.Cell().Element(Celda).AlignRight().Text("Sin servir").SemiBold();
                });
                foreach (var f in filas)
                {
                    tabla.Cell().Element(Celda).Text(f.FechaServicio.ToString("yyyy-MM-dd"));
                    tabla.Cell().Element(Celda).Text(f.TiempoComida);
                    tabla.Cell().Element(Celda).AlignRight().Text(f.Servidas.ToString());
                    tabla.Cell().Element(Celda).AlignRight().Text(f.SinServir.ToString());
                }

                tabla.Cell().Element(Celda).Text("Total").SemiBold();
                tabla.Cell().Element(Celda).Text("");
                tabla.Cell().Element(Celda).AlignRight().Text(filas.Sum(f => f.Servidas).ToString()).SemiBold();
                tabla.Cell().Element(Celda).AlignRight().Text(filas.Sum(f => f.SinServir).ToString()).SemiBold();
            });
        })).GeneratePdf();

    private static IContainer Celda(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
}
