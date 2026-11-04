using Comedor.Domain.Empleados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Comedor.Infrastructure.Persistencia.Configuraciones;

public class EmpleadoConfiguracion : IEntityTypeConfiguration<Empleado>
{
    // Lunes = 1, martes = 2, miércoles = 4, jueves = 8, viernes = 16, sábado = 32, domingo = 64.
    private static readonly DayOfWeek[] Orden =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    private static int ABits(IReadOnlySet<DayOfWeek> dias)
    {
        var bits = 0;
        for (var i = 0; i < Orden.Length; i++)
        {
            if (dias.Contains(Orden[i]))
            {
                bits |= 1 << i;
            }
        }

        return bits;
    }

    private static IReadOnlySet<DayOfWeek> ADias(int bits)
    {
        var dias = new HashSet<DayOfWeek>();
        for (var i = 0; i < Orden.Length; i++)
        {
            if ((bits & (1 << i)) != 0)
            {
                dias.Add(Orden[i]);
            }
        }

        return dias;
    }

    public void Configure(EntityTypeBuilder<Empleado> b)
    {
        b.ToTable("Empleados");
        b.HasKey(e => e.Matricula);
        b.Property(e => e.Matricula).HasMaxLength(20);
        b.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
        b.Property(e => e.Area).HasMaxLength(100).IsRequired();
        b.Property(e => e.Turno).HasMaxLength(50).IsRequired();
        b.Property(e => e.FotoUrl).HasMaxLength(500);

        var convertidor = new ValueConverter<IReadOnlySet<DayOfWeek>, int>(d => ABits(d), v => ADias(v));
        var comparador = new ValueComparer<IReadOnlySet<DayOfWeek>>(
            (x, y) => ABits(x!) == ABits(y!),
            d => ABits(d),
            d => ADias(ABits(d)));
        b.Property(e => e.DiasLaborales)
            .HasConversion(convertidor, comparador)
            .HasColumnType("int");
    }
}
