using Comedor.Domain.Accesos;

namespace Comedor.Domain.Reglas;

public record ResultadoEvaluacion(bool Autorizado, MotivoRechazo? Motivo, ReglaDerecho? Regla, DateOnly? FechaServicio);
