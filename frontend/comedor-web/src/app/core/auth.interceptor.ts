import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const RUTAS_SIN_TOKEN = ['/api/auth/login', '/api/empleados/sesion'];
const RUTAS_EMPLEADO = ['/api/accesos/celular', '/api/accesos/mi-turno'];
const AVISO_SESION_TERMINADA = 'Tu sesión terminó; vuelve a iniciar sesión';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const ruta = this.rutaDe(req.url);
    if (!ruta.startsWith('/api/') || RUTAS_SIN_TOKEN.includes(ruta)) {
      return next.handle(req);
    }

    const esDelEmpleado = RUTAS_EMPLEADO.includes(ruta);
    const token = esDelEmpleado ? this.auth.tokenEmpleado() : this.auth.tokenSistema();
    if (!token) {
      return next.handle(req);
    }
    return next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
      catchError((e) => {
        // Sesión del chef o del administrador revocada o vencida: se borra y se vuelve a iniciar sesión.
        if (!esDelEmpleado && e instanceof HttpErrorResponse && e.status === 401) {
          this.auth.cerrarSesionSistema();
          void this.router.navigate(['/login'], {
            queryParams: { returnUrl: this.router.url },
            state: { aviso: AVISO_SESION_TERMINADA },
          });
        }
        return throwError(() => e);
      }),
    );
  }

  private rutaDe(url: string): string {
    try {
      return new URL(url, 'http://localhost').pathname;
    } catch {
      return url;
    }
  }
}
