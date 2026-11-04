using Comedor.Application.Abstracciones;
using Comedor.Domain.Reglas;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class ReglaRepositorio(ComedorDbContext contexto) : IReglaRepositorio
{
    public async Task<IReadOnlyList<ReglaDerecho>> TodasAsync() =>
        await contexto.Reglas.AsNoTracking().ToListAsync();

    public async Task GuardarAsync(ReglaDerecho r)
    {
        var existente = await contexto.Reglas.FindAsync(r.Turno, r.TiempoComida);
        if (existente is null)
        {
            contexto.Reglas.Add(r);
        }
        else
        {
            contexto.Entry(existente).CurrentValues.SetValues(r);
        }

        await contexto.SaveChangesAsync();
    }

    public async Task EliminarAsync(string turno, string tiempoComida)
    {
        var existente = await contexto.Reglas.FindAsync(turno, tiempoComida);
        if (existente is not null)
        {
            contexto.Reglas.Remove(existente);
            await contexto.SaveChangesAsync();
        }
    }
}
