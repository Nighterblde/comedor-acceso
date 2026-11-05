namespace Comedor.Application.Abstracciones;

public interface IEmisorJwt
{
    string Emitir(string sujeto, string rol, TimeSpan vigencia);
}
