namespace Comedor.Application.Accesos;

public enum EstadoTurno
{
    SinTurno,
    EnFila,
    Sirviendo,
    Servido
}

public record MiTurno(EstadoTurno Estado, int? Posicion);
