namespace Comedor.Application.Sincronizacion;

public record BitacoraSincronizacion(
    Guid Id,
    DateTime Fecha,
    bool Exito,
    int Altas,
    int Cambios,
    int Bajas,
    int Omitidos,
    string? Error);
