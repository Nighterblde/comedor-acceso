using Comedor.Application.Abstracciones;
using Comedor.Application.Reportes;
using Comedor.Domain.Accesos;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class ConsultaReportes(ComedorDbContext contexto) : IConsultaReportes
{
    private const string SinDato = "Sin dato";

    public async Task<IReadOnlyList<ConsumoReporte>> ConsumosAsync(DateOnly desde, DateOnly hasta)
    {
        var filas = await (
            from c in contexto.Consumos.AsNoTracking()
            where c.FechaServicio >= desde && c.FechaServicio <= hasta
            join e in contexto.Empleados.AsNoTracking() on c.Matricula equals e.Matricula into emp
            from e in emp.DefaultIfEmpty()
            select new
            {
                c.FechaServicio,
                c.TiempoComida,
                Turno = e == null ? SinDato : e.Turno,
                Area = e == null ? SinDato : e.Area,
                Servido = c.Canal == Canal.Contingencia || contexto.Entregas.Any(x => x.ConsumoId == c.Id),
            }).ToListAsync();
        return filas.Select(f => new ConsumoReporte(f.FechaServicio, f.TiempoComida, f.Turno, f.Area, f.Servido)).ToList();
    }

    public async Task<IReadOnlyList<FilaDetalle>> ConsumosDetalleAsync(DateOnly desde, DateOnly hasta,
        string? matricula, string? puntoId, Canal? canal)
    {
        var consumos = contexto.Consumos.AsNoTracking().Where(c => c.FechaServicio >= desde && c.FechaServicio <= hasta);
        if (matricula is not null) consumos = consumos.Where(c => c.Matricula == matricula);
        if (puntoId is not null) consumos = consumos.Where(c => c.PuntoId == puntoId);
        if (canal is not null) consumos = consumos.Where(c => c.Canal == canal);

        var filas = await (
            from c in consumos
            join e in contexto.Empleados.AsNoTracking() on c.Matricula equals e.Matricula into emp
            from e in emp.DefaultIfEmpty()
            select new
            {
                c.FechaHora,
                c.Matricula,
                Nombre = e == null ? "" : e.Nombre,
                c.TiempoComida,
                c.PuntoId,
                c.Canal,
                Servido = c.Canal == Canal.Contingencia || contexto.Entregas.Any(x => x.ConsumoId == c.Id),
            }).ToListAsync();
        return filas.Select(f => new FilaDetalle(f.FechaHora, f.Matricula, f.Nombre, f.TiempoComida, f.PuntoId,
            f.Canal, "Autorizado", null, f.Servido)).ToList();
    }

    public async Task<IReadOnlyList<FilaDetalle>> RechazosDetalleAsync(DateOnly desde, DateOnly hasta,
        string? matricula, string? puntoId, Canal? canal, MotivoRechazo? motivo)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var rechazos = contexto.Rechazos.AsNoTracking().Where(r =>
            (r.FechaServicio != null && r.FechaServicio >= desde && r.FechaServicio <= hasta)
            || (r.FechaServicio == null && r.FechaHora >= inicio && r.FechaHora < fin));
        if (matricula is not null) rechazos = rechazos.Where(r => r.Matricula == matricula);
        if (puntoId is not null) rechazos = rechazos.Where(r => r.PuntoId == puntoId);
        if (canal is not null) rechazos = rechazos.Where(r => r.Canal == canal);
        if (motivo is not null) rechazos = rechazos.Where(r => r.Motivo == motivo);

        var filas = await (
            from r in rechazos
            join e in contexto.Empleados.AsNoTracking() on r.Matricula equals e.Matricula into emp
            from e in emp.DefaultIfEmpty()
            select new
            {
                r.FechaHora,
                r.Matricula,
                Nombre = e == null ? "" : e.Nombre,
                r.TiempoComida,
                r.PuntoId,
                r.Canal,
                r.Motivo,
            }).ToListAsync();
        return filas.Select(f => new FilaDetalle(f.FechaHora, f.Matricula, f.Nombre, f.TiempoComida ?? "", f.PuntoId,
            f.Canal, "Rechazado", MotivoRechazoTexto.De(f.Motivo), false)).ToList();
    }
}
