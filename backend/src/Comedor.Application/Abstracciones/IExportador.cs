using Comedor.Application.Reportes;

namespace Comedor.Application.Abstracciones;

public interface IExportador
{
    byte[] CotejoExcel(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta);

    byte[] CotejoPdf(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta);

    byte[] ConteoExcel(IReadOnlyList<FilaConteo> filas, DateOnly desde, DateOnly hasta);

    byte[] ConteoPdf(IReadOnlyList<FilaConteo> filas, DateOnly desde, DateOnly hasta);

    byte[] DetalleExcel(IReadOnlyList<FilaDetalle> filas);
}
