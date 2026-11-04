namespace Comedor.Domain.Accesos;

public static class MotivoRechazoTexto
{
    public static string De(MotivoRechazo motivo) => motivo switch
    {
        MotivoRechazo.NoRegistrado => "No registrado",
        MotivoRechazo.ContratoVencido => "Contrato vencido",
        MotivoRechazo.DiaDescanso => "Hoy es tu día de descanso",
        MotivoRechazo.FueraDeHorario => "Fuera de horario",
        MotivoRechazo.TurnoSinDerecho => "Tu turno no tiene derecho a este tiempo de comida",
        MotivoRechazo.YaConsumio => "Ya consumiste este tiempo de comida",
        _ => throw new ArgumentOutOfRangeException(nameof(motivo), motivo, null)
    };
}
