using Comedor.Application.Abstracciones;
using Comedor.Application.Qr;
using Comedor.Domain.Accesos;
using Comedor.Domain.Reglas;

namespace Comedor.Application.Accesos;

public class ValidarAcceso(
    IEmpleadoRepositorio empleados,
    IReglaRepositorio reglas,
    IAccesoRepositorio accesos,
    ValidadorTokenQr validadorQr,
    IReloj reloj)
{
    public const string TextoEscanearDeNuevo = "Escanea de nuevo el código de la entrada";

    public async Task<ResultadoAcceso> PorCelularAsync(string matricula, string tokenQr)
    {
        var ahora = reloj.Ahora();
        var normalizada = Normalizar(matricula);

        var puntoId = await validadorQr.ValidarAsync(tokenQr);
        if (puntoId is null)
        {
            return new ResultadoAcceso(EstadoAcceso.EscanearDeNuevo, TextoEscanearDeNuevo, normalizada,
                null, null, null, null, ahora);
        }

        return await EvaluarAsync(normalizada, puntoId, Canal.Celular, null, ahora);
    }

    public Task<ResultadoAcceso> ManualAsync(string matricula, string puntoId, string encargado) =>
        EvaluarAsync(Normalizar(matricula), puntoId, Canal.Manual, encargado, reloj.Ahora());

    private static string Normalizar(string? matricula) => (matricula ?? "").Trim().ToUpperInvariant();

    private async Task<ResultadoAcceso> EvaluarAsync(
        string matricula, string puntoId, Canal canal, string? encargado, DateTime ahora)
    {
        var empleado = await empleados.ObtenerAsync(matricula);
        var todas = await reglas.TodasAsync();

        // La verificación de consumo previo es asíncrona y es la última del evaluador.
        var resultado = EvaluadorDerecho.Evaluar(empleado, todas, ahora, (_, _) => false);
        if (resultado.Autorizado
            && await accesos.YaConsumioAsync(matricula, resultado.FechaServicio!.Value, resultado.Regla!.TiempoComida))
        {
            resultado = new ResultadoEvaluacion(false, MotivoRechazo.YaConsumio, resultado.Regla, resultado.FechaServicio);
        }

        if (!resultado.Autorizado)
        {
            return await RechazarAsync(resultado.Motivo!.Value, resultado.Regla, resultado.FechaServicio);
        }

        var regla = resultado.Regla!;
        var fecha = resultado.FechaServicio!.Value;
        try
        {
            await accesos.RegistrarConsumoAsync(
                Consumo.Crear(matricula, fecha, regla.TiempoComida, ahora, puntoId, canal, encargado));
        }
        catch (ConsumoDuplicadoException)
        {
            return await RechazarAsync(MotivoRechazo.YaConsumio, regla, fecha);
        }

        return Resultado(EstadoAcceso.Autorizado, null, regla);

        async Task<ResultadoAcceso> RechazarAsync(MotivoRechazo motivo, ReglaDerecho? reglaRechazo, DateOnly? fechaRechazo)
        {
            await accesos.RegistrarRechazoAsync(Rechazo.Crear(matricula, ahora, fechaRechazo,
                reglaRechazo?.TiempoComida, puntoId, canal, encargado, motivo));
            return Resultado(EstadoAcceso.Rechazado, MotivoRechazoTexto.De(motivo), reglaRechazo);
        }

        ResultadoAcceso Resultado(EstadoAcceso estado, string? motivo, ReglaDerecho? reglaCoincidente) =>
            new(estado, motivo, matricula, empleado?.Nombre, empleado?.Turno, reglaCoincidente?.TiempoComida,
                empleado?.FotoUrl, ahora);
    }
}
