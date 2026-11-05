using Comedor.Application.Abstracciones;

namespace Comedor.Infrastructure;

public class RelojZonaHoraria : IReloj
{
    private readonly TimeZoneInfo _zona;
    private readonly TimeProvider _proveedor;

    public RelojZonaHoraria(string zonaHorariaId, TimeProvider? proveedor = null)
    {
        _zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);
        _proveedor = proveedor ?? TimeProvider.System;
    }

    public DateTime Ahora() => TimeZoneInfo.ConvertTime(_proveedor.GetUtcNow(), _zona).DateTime;
}
