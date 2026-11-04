using Comedor.Application.Abstracciones;
using Comedor.Domain.Empleados;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Comedor.Infrastructure.Repositorios;

public class EmpleadoRepositorio(ComedorDbContext contexto) : IEmpleadoRepositorio
{
    public Task<Empleado?> ObtenerAsync(string matricula) =>
        contexto.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Matricula == matricula);

    public async Task<IReadOnlyList<Empleado>> TodosAsync() =>
        await contexto.Empleados.AsNoTracking().ToListAsync();

    public async Task GuardarAsync(IEnumerable<Empleado> empleados)
    {
        foreach (var empleado in empleados)
        {
            var existente = await contexto.Empleados.FindAsync(empleado.Matricula);
            if (existente is null)
            {
                contexto.Empleados.Add(empleado);
            }
            else
            {
                contexto.Entry(existente).CurrentValues.SetValues(empleado);
            }
        }

        await contexto.SaveChangesAsync();
    }
}
