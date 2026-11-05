using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class UsuarioSistemaRepositorio(ComedorDbContext contexto) : IUsuarioSistemaRepositorio
{
    public Task<UsuarioSistema?> ObtenerAsync(string usuario) =>
        contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Usuario == usuario);

    public async Task<IReadOnlyList<UsuarioSistema>> TodosAsync() =>
        await contexto.Usuarios.AsNoTracking().OrderBy(u => u.Usuario).ToListAsync();

    public async Task GuardarAsync(UsuarioSistema u)
    {
        var existente = await contexto.Usuarios.FindAsync(u.Usuario);
        if (existente is null)
        {
            contexto.Usuarios.Add(u);
        }
        else
        {
            contexto.Entry(existente).CurrentValues.SetValues(u);
        }

        await contexto.SaveChangesAsync();
    }

    public Task<bool> ExisteAlgunoAsync() => contexto.Usuarios.AnyAsync();
}
