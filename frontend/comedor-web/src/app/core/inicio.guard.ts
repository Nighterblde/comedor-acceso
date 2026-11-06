import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Ruta /inicio (accesos directos): lleva al inicio guardado del rol, o al login si no hay sesión. */
export const inicioGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const inicio = auth.inicio();
  if (auth.tokenSistema() && inicio) {
    return router.parseUrl(inicio);
  }
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: '/inicio' } });
};
