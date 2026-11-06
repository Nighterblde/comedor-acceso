using Comedor.Domain.Reglas;

namespace Comedor.Application.Accesos;

/// <summary>Determina si un tiempo de comida y su fecha de servicio son los del horario en curso.</summary>
public static class HorarioEnCurso
{
    public static bool Contiene(IReadOnlyList<ReglaDerecho> reglas, DateTime ahora, string tiempoComida,
        DateOnly fechaServicio)
    {
        var hora = TimeOnly.FromDateTime(ahora);
        return reglas.Any(r => r.TiempoComida == tiempoComida && r.Contiene(hora)
            && r.FechaServicio(ahora) == fechaServicio);
    }
}
