namespace Comedor.Application.Sincronizacion;

public record ResultadoSincronizacion(bool Exito, int Altas, int Cambios, int Bajas, int Omitidos, string? Error);
