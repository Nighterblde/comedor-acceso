namespace Comedor.Domain.Reglas;

public record ReglaDerecho(string Turno, string TiempoComida, TimeOnly Inicio, TimeOnly Fin,
    bool PerteneceAJornadaAnterior = false)
{
    public bool CruzaMedianoche => Fin < Inicio;

    /// <summary>Fin exclusivo.</summary>
    public bool Contiene(TimeOnly hora) =>
        CruzaMedianoche ? hora >= Inicio || hora < Fin : hora >= Inicio && hora < Fin;

    public bool SeTraslapaCon(ReglaDerecho otra)
    {
        if (Turno != otra.Turno)
        {
            return false;
        }

        foreach (var propio in Segmentos())
        {
            foreach (var ajeno in otra.Segmentos())
            {
                if (propio.Desde < ajeno.Hasta && ajeno.Desde < propio.Hasta)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Dia en que inicia el horario en curso; un dia menos si pertenece a la jornada anterior.</summary>
    public DateOnly FechaServicio(DateTime ahora)
    {
        var fecha = DateOnly.FromDateTime(ahora);
        if (CruzaMedianoche && TimeOnly.FromDateTime(ahora) < Inicio)
        {
            fecha = fecha.AddDays(-1);
        }

        return PerteneceAJornadaAnterior ? fecha.AddDays(-1) : fecha;
    }

    private IEnumerable<(int Desde, int Hasta)> Segmentos()
    {
        var desde = (int)(Inicio.Ticks / TimeSpan.TicksPerMinute);
        var hasta = (int)(Fin.Ticks / TimeSpan.TicksPerMinute);
        if (CruzaMedianoche)
        {
            yield return (desde, 1440);
            yield return (0, hasta);
        }
        else
        {
            yield return (desde, hasta);
        }
    }
}
