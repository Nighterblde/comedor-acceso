export interface SolicitudPunto {
  consumoId: string | null;
  fechaHora: string;
  matricula: string;
  nombre: string | null;
  fotoUrl: string | null;
  turno: string | null;
  tiempoComida: string | null;
  fechaServicio: string | null;
  autorizado: boolean;
  motivo: string | null;
  servido: boolean;
}
