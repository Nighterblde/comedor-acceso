import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function rolGuard(roles: string[]): CanActivateFn {
  return (_ruta, estado) => {
    const rol = inject(AuthService).rol();
    if (rol !== null && roles.includes(rol)) {
      return true;
    }
    return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: estado.url } });
  };
}
