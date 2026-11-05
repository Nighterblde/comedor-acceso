namespace Comedor.Application.Abstracciones;

public class EntregaDuplicadaException : Exception
{
    public EntregaDuplicadaException(Exception? interna = null)
        : base("El consumo ya fue entregado", interna)
    {
    }
}
