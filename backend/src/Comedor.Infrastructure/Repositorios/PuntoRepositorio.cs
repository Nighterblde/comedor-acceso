using Comedor.Application.Abstracciones;
using Comedor.Domain.Puntos;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class PuntoRepositorio(ComedorDbContext contexto) : IPuntoRepositorio
{
    public Task<PuntoEntrada?> ObtenerAsync(string id) =>
        contexto.Puntos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<PuntoEntrada>> TodosAsync() =>
        await contexto.Puntos.AsNoTracking().ToListAsync();

    public async Task GuardarAsync(PuntoEntrada p)
    {
        var existente = await contexto.Puntos.FindAsync(p.Id);
        if (existente is null)
        {
            contexto.Puntos.Add(p);
        }
        else
        {
            contexto.Entry(existente).CurrentValues.SetValues(p);
        }

        await contexto.SaveChangesAsync();
    }
}
