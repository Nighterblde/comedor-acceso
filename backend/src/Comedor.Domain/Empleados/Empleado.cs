namespace Comedor.Domain.Empleados;

public record Empleado(
    string Matricula,
    string Nombre,
    string Area,
    string Turno,
    IReadOnlySet<DayOfWeek> DiasLaborales,
    DateOnly InicioContrato,
    DateOnly FinContrato,
    string? FotoUrl,
    bool Activo);
