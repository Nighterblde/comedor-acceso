/** Horas en formato HH:mm. */
export interface ReglaDerecho {
  turno: string;
  tiempoComida: string;
  inicio: string;
  fin: string;
  perteneceAJornadaAnterior: boolean;
}

export interface PuntoEntrada {
  id: string;
  nombre: string;
  activo: boolean;
}

/** Vista de un usuario del sistema; nunca incluye la contraseña. */
export interface UsuarioSistema {
  usuario: string;
  rol: string;
  activo: boolean;
}

/** Alta o edición de un usuario; la contraseña es obligatoria al crear y opcional al editar. */
export interface UsuarioEdicion extends UsuarioSistema {
  contrasena?: string;
}

export interface ResultadoSincronizacion {
  exito: boolean;
  altas: number;
  cambios: number;
  bajas: number;
  omitidos: number;
  error: string | null;
}

export interface BitacoraSincronizacion extends ResultadoSincronizacion {
  id: string;
  fecha: string;
}

/** Consumo de contingencia como lo guardó la API (matrícula normalizada). */
export interface ConsumoContingencia {
  id: string;
  matricula: string;
  fechaServicio: string;
  tiempoComida: string;
}

/** Fecha de servicio en formato aaaa-MM-dd. */
export interface Contingencia {
  matricula: string;
  fechaServicio: string;
  tiempoComida: string;
  referenciaLista: string;
}
