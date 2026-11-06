import { InjectionToken } from '@angular/core';
import { ReglaDerecho } from '../modelos/administracion.model';

/** Hora local del navegador; se sustituye en las pruebas. */
export const AHORA = new InjectionToken<() => Date>('AHORA', { providedIn: 'root', factory: () => () => new Date() });

function minutos(hhmm: string): number {
  const [h, m] = hhmm.split(':').map(Number);
  return h * 60 + m;
}

/** Hay horario de comida si la hora cae en alguna regla; el fin es exclusivo y puede cruzar la medianoche. */
export function enHorarioDeComida(reglas: ReglaDerecho[], ahora: Date): boolean {
  const actual = ahora.getHours() * 60 + ahora.getMinutes();
  return reglas.some((r) => {
    const inicio = minutos(r.inicio);
    const fin = minutos(r.fin);
    return fin < inicio ? actual >= inicio || actual < fin : actual >= inicio && actual < fin;
  });
}
