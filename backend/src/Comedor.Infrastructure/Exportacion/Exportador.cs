using Comedor.Application.Abstracciones;
using Comedor.Application.Reportes;

namespace Comedor.Infrastructure.Exportacion;

public class Exportador : IExportador
{
    public byte[] CotejoExcel(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta) =>
        ExportadorExcel.Cotejo(filas);

    public byte[] CotejoPdf(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta) =>
        ExportadorPdf.Cotejo(filas, desde, hasta);

    public byte[] ConteoExcel(IReadOnlyList<FilaConteo> filas, DateOnly desde, DateOnly hasta) =>
        ExportadorExcel.Conteo(filas);

    public byte[] ConteoPdf(IReadOnlyList<FilaConteo> filas, DateOnly desde, DateOnly hasta) =>
        ExportadorPdf.Conteo(filas, desde, hasta);

    public byte[] DetalleExcel(IReadOnlyList<FilaDetalle> filas) => ExportadorExcel.Detalle(filas);
}
