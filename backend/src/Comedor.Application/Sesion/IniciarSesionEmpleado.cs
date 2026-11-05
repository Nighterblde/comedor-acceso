using Comedor.Application.Abstracciones;

namespace Comedor.Application.Sesion;

public enum ErrorSesion
{
    CredencialesIncorrectas,
    Bloqueado,
    ServicioNoDisponible
}

public record ResultadoSesion(bool Exito, string? Token, ErrorSesion? Error);

public class IniciarSesionEmpleado
{
    private readonly IAutenticadorEmpresa _autenticador;
    private readonly IEmisorJwt _emisor;
    private readonly ControlIntentos _intentos;
    private readonly OpcionesSesion _opciones;

    public IniciarSesionEmpleado(
        IAutenticadorEmpresa autenticador,
        IEmisorJwt emisor,
        ControlIntentos intentos,
        OpcionesSesion opciones)
    {
        _autenticador = autenticador;
        _emisor = emisor;
        _intentos = intentos;
        _opciones = opciones;
    }

    public async Task<ResultadoSesion> EjecutarAsync(string id, string contrasena)
    {
        var matricula = id.Trim().ToUpperInvariant();

        if (_intentos.EstaBloqueado(matricula))
        {
            return new ResultadoSesion(false, null, ErrorSesion.Bloqueado);
        }

        bool valido;
        try
        {
            valido = await _autenticador.ValidarAsync(matricula, contrasena);
        }
        catch (Exception)
        {
            return new ResultadoSesion(false, null, ErrorSesion.ServicioNoDisponible);
        }

        if (!valido)
        {
            _intentos.RegistrarFallo(matricula);
            return new ResultadoSesion(false, null, ErrorSesion.CredencialesIncorrectas);
        }

        _intentos.Limpiar(matricula);
        var token = _emisor.Emitir(matricula, "Empleado", TimeSpan.FromDays(_opciones.DiasEmpleado));
        return new ResultadoSesion(true, token, null);
    }
}
