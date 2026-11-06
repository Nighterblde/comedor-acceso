using Comedor.Application.Abstracciones;

namespace Comedor.Infrastructure.Externos;

internal static class FilaInvalida
{
    /// <summary>
    /// Fila cuyos días o fechas no se pudieron leer (D-49). Conserva la matrícula y lleva el turno vacío, que
    /// no está en el catálogo: <c>SincronizarPersonal</c> la cuenta como omitida y deja al empleado como estaba (D-28).
    /// </summary>
    public static EmpleadoFuente Para(string? matricula) =>
        new(matricula ?? string.Empty, string.Empty, string.Empty, string.Empty, new HashSet<DayOfWeek>(),
            DateOnly.MinValue, DateOnly.MinValue, null);
}
