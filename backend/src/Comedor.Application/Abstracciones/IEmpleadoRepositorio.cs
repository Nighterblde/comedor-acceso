using Comedor.Domain.Empleados;

namespace Comedor.Application.Abstracciones;

public interface IEmpleadoRepositorio
{
    Task<Empleado?> ObtenerAsync(string matricula);

    Task<IReadOnlyList<Empleado>> TodosAsync();

    /// <summary>Inserta o actualiza por matrícula.</summary>
    Task GuardarAsync(IEnumerable<Empleado> empleados);
}
