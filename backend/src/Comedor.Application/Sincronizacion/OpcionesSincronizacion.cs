namespace Comedor.Application.Sincronizacion;

public class OpcionesSincronizacion
{
    /// <summary>Catálogo de turnos válidos; un turno fuera de él omite el registro.</summary>
    public IReadOnlyList<string> Turnos { get; set; } = ["Matutino", "Vespertino", "Nocturno"];

    /// <summary>Hora local a la que corre la sincronización nocturna.</summary>
    public TimeOnly Hora { get; set; } = new(2, 0);
}
