namespace Comedor.Infrastructure.Externos;

/// <summary>Convierte el texto de días laborales de RR. HH. ("L,M,X,J,V") en un conjunto de días (D-42).</summary>
public static class DiasLaboralesTexto
{
    /// <summary>
    /// Letras L, M, X, J, V, S y D (lunes a domingo) separadas por coma; se ignoran espacios y mayúsculas.
    /// Una letra desconocida, un elemento vacío o un texto vacío lanzan <see cref="FormatException"/> (D-49).
    /// </summary>
    public static IReadOnlySet<DayOfWeek> Parse(string texto)
    {
        var dias = new HashSet<DayOfWeek>();
        foreach (var parte in (texto ?? string.Empty).Split(','))
        {
            var letra = parte.Trim().ToUpperInvariant();
            dias.Add(letra switch
            {
                "L" => DayOfWeek.Monday,
                "M" => DayOfWeek.Tuesday,
                "X" => DayOfWeek.Wednesday,
                "J" => DayOfWeek.Thursday,
                "V" => DayOfWeek.Friday,
                "S" => DayOfWeek.Saturday,
                "D" => DayOfWeek.Sunday,
                _ => throw new FormatException($"Día laboral desconocido: '{parte.Trim()}'. Usa L, M, X, J, V, S o D."),
            });
        }

        return dias;
    }
}
