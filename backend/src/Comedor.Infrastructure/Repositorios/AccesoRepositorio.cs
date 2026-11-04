using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Domain.Accesos;
using Comedor.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class AccesoRepositorio(ComedorDbContext contexto) : IAccesoRepositorio
{
    public Task<bool> YaConsumioAsync(string matricula, DateOnly fechaServicio, string tiempoComida) =>
        contexto.Consumos.AnyAsync(c =>
            c.Matricula == matricula && c.FechaServicio == fechaServicio && c.TiempoComida == tiempoComida);

    public async Task RegistrarConsumoAsync(Consumo c)
    {
        contexto.Consumos.Add(c);
        try
        {
            await contexto.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsClaveDuplicada(ex))
        {
            contexto.Entry(c).State = EntityState.Detached;
            throw new ConsumoDuplicadoException(ex);
        }
    }

    public async Task RegistrarRechazoAsync(Rechazo r)
    {
        contexto.Rechazos.Add(r);
        await contexto.SaveChangesAsync();
    }

    public async Task RegistrarEntregaAsync(Entrega e)
    {
        contexto.Entregas.Add(e);
        try
        {
            await contexto.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsClaveDuplicada(ex))
        {
            contexto.Entry(e).State = EntityState.Detached;
            throw new EntregaDuplicadaException(ex);
        }
    }

    public Task<Consumo?> ObtenerConsumoAsync(Guid id) =>
        contexto.Consumos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> EstaServidoAsync(Guid consumoId) =>
        contexto.Entregas.AnyAsync(e => e.ConsumoId == consumoId);

    public async Task<IReadOnlyList<SolicitudPunto>> SolicitudesAsync(string puntoId, DateTime desde)
    {
        var consumos = await (
            from c in contexto.Consumos.AsNoTracking()
            where c.PuntoId == puntoId && c.FechaHora >= desde
            join e in contexto.Empleados.AsNoTracking() on c.Matricula equals e.Matricula into grupo
            from e in grupo.DefaultIfEmpty()
            select new SolicitudPunto(
                c.Id,
                c.FechaHora,
                c.Matricula,
                e.Nombre,
                e.FotoUrl,
                e.Turno,
                c.TiempoComida,
                c.FechaServicio,
                true,
                null,
                contexto.Entregas.Any(x => x.ConsumoId == c.Id))).ToListAsync();

        var rechazos = await (
            from r in contexto.Rechazos.AsNoTracking()
            where r.PuntoId == puntoId && r.FechaHora >= desde
            join e in contexto.Empleados.AsNoTracking() on r.Matricula equals e.Matricula into grupo
            from e in grupo.DefaultIfEmpty()
            select new
            {
                r.FechaHora,
                r.Matricula,
                e.Nombre,
                e.FotoUrl,
                e.Turno,
                r.TiempoComida,
                r.FechaServicio,
                r.Motivo
            }).ToListAsync();

        return consumos
            .Concat(rechazos.Select(r => new SolicitudPunto(
                null, r.FechaHora, r.Matricula, r.Nombre, r.FotoUrl, r.Turno, r.TiempoComida, r.FechaServicio,
                false, MotivoRechazoTexto.De(r.Motivo), false)))
            .OrderByDescending(s => s.FechaHora)
            .ToList();
    }

    private static bool EsClaveDuplicada(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
