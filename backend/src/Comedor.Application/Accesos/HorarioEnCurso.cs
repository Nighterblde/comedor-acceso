using Comedor.Domain.Reglas;

namespace Comedor.Application.Accesos;

/// <summary>Determina si el turno, el tiempo de comida y la fecha de servicio son los del horario en curso.</summary>
public static class HorarioEnCurso
{
    public static bool Contiene(IReadOnlyList<ReglaDerecho> reglas, DateTime ahora, string? turno, string tiempoComida,
        DateOnly fechaServicio)
    {
        var hora = TimeOnly.FromDateTime(ahora);
        return turno is not null && reglas.Any(r => r.Turno == turno && r.TiempoComida == tiempoComida && r.Contiene(hora)
            && r.FechaServicio(ahora) == fechaServicio);
    }
}
