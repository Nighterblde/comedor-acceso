import { HttpErrorResponse } from '@angular/common/http';

/** Mensaje genérico para errores inesperados (SRS §8). */
export const MENSAJE_GENERICO = 'Ocurrió un error; intenta de nuevo.';

/** Texto que la API envía en `{ error }` o en `title` (ProblemDetails); si no hay, el genérico. */
export function textoDeError(err: unknown): string {
  if (err instanceof HttpErrorResponse && err.error && typeof err.error === 'object') {
    const cuerpo = err.error as { error?: unknown; title?: unknown };
    if (typeof cuerpo.error === 'string') {
      return cuerpo.error;
    }
    if (typeof cuerpo.title === 'string' && err.status === 400) {
      return cuerpo.title;
    }
  }
  return MENSAJE_GENERICO;
}
