export type Canal = 'Celular' | 'Manual' | 'Contingencia';

export interface FilaCotejo {
  fechaServicio: string;
  tiempoComida: string;
  servidas: number;
  sinServir: number;
}

export interface FilaConteo {
  fechaServicio: string;
  tiempoComida: string;
  turno: string;
  area: string;
  total: number;
}

export interface FilaDetalle {
  fechaHora: string;
  matricula: string;
  nombre: string;
  tiempoComida: string;
  puntoId: string;
  canal: Canal;
  resultado: string;
  motivo: string | null;
  servido: boolean;
}
