import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AdminService } from '../admin/admin.service';
import { AHORA, enHorarioDeComida } from './horario';

/** /panel abre la fila durante un horario de comida y el cotejo fuera de él (D-37). */
export const panelInicioGuard: CanActivateFn = () => {
  const router = inject(Router);
  const ahora = inject(AHORA);
  return inject(AdminService)
    .reglas()
    .pipe(
      map((reglas) => (enHorarioDeComida(reglas, ahora()) ? 'fila' : 'cotejo')),
      // Si no se pueden leer las reglas, se abre el cotejo: no depende del horario.
      catchError(() => of('cotejo')),
      map((destino) => router.createUrlTree(['/panel', destino])),
    );
};
