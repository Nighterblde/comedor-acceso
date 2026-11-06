using System.Globalization;
using ClosedXML.Excel;
using Comedor.Application.Abstracciones;

namespace Comedor.Infrastructure.Externos;

/// <summary>
/// Fuente de RR. HH. que lee un Excel (primera hoja, fila 1 = encabezados). Solo lee el archivo (RF-03, D-42).
/// </summary>
public sealed class FuenteRRHHExcel(string ruta) : IFuentePersonalRRHH
{
    public static readonly IReadOnlyList<string> Columnas =
        ["Matricula", "Nombre", "Area", "Turno", "DiasLaborales", "InicioContrato", "FinContrato", "FotoUrl"];

    private static readonly string[] FormatosFecha = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"];

    public Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync()
    {
        // Se abre el flujo aquí para liberarlo siempre, incluso si el archivo está dañado.
        using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var libro = new XLWorkbook(flujo);
        var hoja = libro.Worksheets.FirstOrDefault()
                   ?? throw new InvalidDataException("El archivo de personal no tiene hojas.");

        var ultimaColumna = hoja.LastColumnUsed()?.ColumnNumber() ?? 0;
        var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = 1; c <= ultimaColumna; c++)
        {
            columnas.TryAdd(hoja.Cell(1, c).GetString().Trim(), c);
        }

        var faltan = Columnas.Where(n => n != "FotoUrl" && !columnas.ContainsKey(n)).ToList();
        if (faltan.Count > 0)
        {
            throw new InvalidDataException("Faltan columnas en el Excel de personal: " + string.Join(", ", faltan));
        }

        var filas = new List<EmpleadoFuente>();
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= ultimaFila; r++)
        {
            var fila = r;
            string Texto(string columna) =>
                columnas.TryGetValue(columna, out var c) ? hoja.Cell(fila, c).GetFormattedString().Trim() : string.Empty;

            if (Columnas.All(n => Texto(n).Length == 0))
            {
                continue; // fila en blanco: no cuenta como omitida
            }

            var matricula = Texto("Matricula");
            try
            {
                var foto = Texto("FotoUrl");
                filas.Add(new EmpleadoFuente(
                    matricula,
                    Texto("Nombre"),
                    Texto("Area"),
                    Texto("Turno"),
                    DiasLaboralesTexto.Parse(Texto("DiasLaborales")),
                    Fecha(hoja.Cell(fila, columnas["InicioContrato"])),
                    Fecha(hoja.Cell(fila, columnas["FinContrato"])),
                    foto.Length == 0 ? null : foto));
            }
            catch (FormatException)
            {
                filas.Add(FilaInvalida.Para(matricula));
            }
        }

        return Task.FromResult<IReadOnlyList<EmpleadoFuente>>(filas);
    }

    private static DateOnly Fecha(IXLCell celda)
    {
        if (celda.DataType == XLDataType.DateTime)
        {
            return DateOnly.FromDateTime(celda.GetDateTime());
        }

        var texto = celda.GetFormattedString().Trim();
        if (DateOnly.TryParseExact(texto, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
        {
            return f;
        }

        throw new FormatException($"Fecha inválida: '{texto}'.");
    }
}
