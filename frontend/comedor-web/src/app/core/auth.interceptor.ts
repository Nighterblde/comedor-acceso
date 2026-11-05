import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

const RUTAS_SIN_TOKEN = ['/api/auth/login', '/api/empleados/sesion'];
const RUTA_EMPLEADO = '/api/accesos/celular';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly auth: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const ruta = this.rutaDe(req.url);
    if (!ruta.startsWith('/api/') || RUTAS_SIN_TOKEN.includes(ruta)) {
      return next.handle(req);
    }

    const token = ruta === RUTA_EMPLEADO ? this.auth.tokenEmpleado() : this.auth.tokenSistema();
    if (!token) {
      return next.handle(req);
    }
    return next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  }

  private rutaDe(url: string): string {
    try {
      return new URL(url, 'http://localhost').pathname;
    } catch {
      return url;
    }
  }
}
