using Comedor.Application.Abstracciones;

namespace Comedor.Application.Accesos;

public class ConsultarMiTurno(IAccesoRepositorio accesos, IReglaRepositorio reglas, IPuntoRepositorio puntos, IReloj reloj)
{
    public async Task<MiTurno> EjecutarAsync(string matricula)
    {
        var ahora = reloj.Ahora();
        var todas = await reglas.TodasAsync();
        var desde = ahora - ConsultarFila.Ventana;

        // El consumo autorizado más reciente de la matrícula en el horario en curso, en cualquier punto.
        SolicitudPunto? suyo = null;
        IReadOnlyList<SolicitudPunto> filasDelPunto = [];
        foreach (var punto in await puntos.TodosAsync())
        {
            var filas = await accesos.SolicitudesAsync(punto.Id, desde);
            var candidato = filas
                .Where(f => f.Autorizado && f.Matricula == matricula && f.TiempoComida is not null
                    && f.FechaServicio is not null
                    && HorarioEnCurso.Contiene(todas, ahora, f.Turno, f.TiempoComida, f.FechaServicio.Value))
                .OrderByDescending(f => f.FechaHora)
                .FirstOrDefault();
            if (candidato is not null && (suyo is null || candidato.FechaHora > suyo.FechaHora))
            {
                suyo = candidato;
                filasDelPunto = filas;
            }
        }

        if (suyo is null)
        {
            return new MiTurno(EstadoTurno.SinTurno, null);
        }

        if (suyo.Servido)
        {
            return new MiTurno(EstadoTurno.Servido, null);
        }

        var espera = ConsultarFila.Esperando(filasDelPunto, todas, ahora);
        var posicion = espera.ToList().FindIndex(f => f.ConsumoId == suyo.ConsumoId) + 1;
        return posicion == 1
            ? new MiTurno(EstadoTurno.Sirviendo, 1)
            : new MiTurno(EstadoTurno.EnFila, posicion);
    }
}
