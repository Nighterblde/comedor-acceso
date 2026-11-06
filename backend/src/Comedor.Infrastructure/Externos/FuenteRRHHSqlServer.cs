using System.Text.RegularExpressions;
using Comedor.Application.Abstracciones;
using Microsoft.Data.SqlClient;

namespace Comedor.Infrastructure.Externos;

/// <summary>Fuente de RR. HH. que lee una vista de SQL Server con columnas fijas. Solo SELECT (RF-03, D-42).</summary>
public sealed partial class FuenteRRHHSqlServer : IFuentePersonalRRHH
{
    private readonly string _cadena;
    private readonly string _vista;

    public FuenteRRHHSqlServer(string cadena, string vista)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new ArgumentException("Falta la cadena de conexión de la fuente de RR. HH.", nameof(cadena));
        }

        // El nombre se concatena en el SELECT: solo se aceptan identificadores (esquema.vista, corchetes).
        if (string.IsNullOrEmpty(vista) || !NombreValido().IsMatch(vista))
        {
            throw new ArgumentException("El nombre de la vista de RR. HH. no es válido.", nameof(vista));
        }

        _cadena = cadena;
        _vista = vista;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_.\[\]]+$")]
    private static partial Regex NombreValido();

    public async Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync()
    {
        var filas = new List<EmpleadoFuente>();
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText =
            $"SELECT Matricula, Nombre, Area, Turno, DiasLaborales, InicioContrato, FinContrato, FotoUrl FROM {_vista}";
        await using var lector = await cmd.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            var matricula = Texto(lector, 0);
            try
            {
                var foto = Texto(lector, 7);
                filas.Add(new EmpleadoFuente(
                    matricula,
                    Texto(lector, 1),
                    Texto(lector, 2),
                    Texto(lector, 3),
                    DiasLaboralesTexto.Parse(Texto(lector, 4)),
                    Fecha(lector, 5),
                    Fecha(lector, 6),
                    foto.Length == 0 ? null : foto));
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException)
            {
                filas.Add(FilaInvalida.Para(matricula));
            }
        }

        return filas;
    }

    private static string Texto(SqlDataReader lector, int i) =>
        lector.IsDBNull(i) ? string.Empty : Convert.ToString(lector.GetValue(i))!.Trim();

    private static DateOnly Fecha(SqlDataReader lector, int i)
    {
        if (lector.IsDBNull(i))
        {
            throw new FormatException("Fecha vacía.");
        }

        return DateOnly.FromDateTime(lector.GetDateTime(i));
    }
}
