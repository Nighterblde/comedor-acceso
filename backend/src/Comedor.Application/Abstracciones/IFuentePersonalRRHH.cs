namespace Comedor.Application.Abstracciones;

public record EmpleadoFuente(
    string Matricula,
    string Nombre,
    string Area,
    string Turno,
    IReadOnlySet<DayOfWeek> DiasLaborales,
    DateOnly InicioContrato,
    DateOnly FinContrato,
    string? FotoUrl);

/// <summary>Fuente de RR. HH. de solo lectura: el sistema nunca escribe en ella (RF-03).</summary>
public interface IFuentePersonalRRHH
{
    Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync();
}
