namespace Comedor.Application.Abstracciones;

public class ConsumoDuplicadoException : Exception
{
    public ConsumoDuplicadoException(Exception? interna = null)
        : base("Ya consumiste este tiempo de comida", interna)
    {
    }
}
