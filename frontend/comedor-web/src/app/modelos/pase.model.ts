export type EstadoAcceso = 'Autorizado' | 'Rechazado' | 'EscanearDeNuevo';

export interface ResultadoAcceso {
  estado: EstadoAcceso;
  motivo: string | null;
  matricula: string | null;
  nombre: string | null;
  turno: string | null;
  tiempoComida: string | null;
  fotoUrl: string | null;
  hora: string;
}

export interface TokenQrRespuesta {
  token: string;
  url: string;
}

export interface RespuestaLogin {
  token: string;
  rol: string;
  expiraEn: string;
}

export interface RespuestaSesionEmpleado {
  token: string;
}
