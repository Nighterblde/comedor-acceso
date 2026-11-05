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
