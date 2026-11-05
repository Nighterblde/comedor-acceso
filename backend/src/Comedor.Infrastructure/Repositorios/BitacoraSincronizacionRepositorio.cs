using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class BitacoraSincronizacionRepositorio(ComedorDbContext contexto) : IBitacoraSincronizacionRepositorio
{
    public async Task RegistrarAsync(BitacoraSincronizacion fila)
    {
        contexto.Bitacora.Add(fila);
        await contexto.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BitacoraSincronizacion>> UltimasAsync(int cantidad) =>
        await contexto.Bitacora.AsNoTracking().OrderByDescending(b => b.Fecha).Take(cantidad).ToListAsync();

    public async Task<BitacoraSincronizacion?> UltimaExitosaAsync() =>
        await contexto.Bitacora.AsNoTracking().Where(b => b.Exito).OrderByDescending(b => b.Fecha).FirstOrDefaultAsync();
}
