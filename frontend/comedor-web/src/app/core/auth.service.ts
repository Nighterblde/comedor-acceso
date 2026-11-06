import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { RespuestaLogin, RespuestaSesionEmpleado } from '../modelos/pase.model';
import { borrar, guardar, leer } from './almacen';

const CLAVE_TOKEN_SISTEMA = 'comedor.token.sistema';
const CLAVE_TOKEN_EMPLEADO = 'comedor.token.empleado';
const CLAVE_ROL = 'comedor.rol';
const CLAVE_USUARIO = 'comedor.usuario';
const CLAVE_INICIO = 'comedor.inicio';
const CLAVE_EXPIRA = 'comedor.expira';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private readonly http: HttpClient) {}

  login(usuario: string, contrasena: string): Observable<void> {
    return this.http
      .post<RespuestaLogin>(`${environment.apiUrl}/api/auth/login`, { usuario, contrasena })
      .pipe(
        tap((r) => {
          guardar(CLAVE_TOKEN_SISTEMA, r.token);
          guardar(CLAVE_ROL, r.rol);
          guardar(CLAVE_USUARIO, usuario.trim());
          guardar(CLAVE_INICIO, r.inicio);
          guardar(CLAVE_EXPIRA, r.expiraEn);
        }),
        map(() => undefined),
      );
  }

  iniciarSesionEmpleado(id: string, contrasena: string): Observable<void> {
    return this.http
      .post<RespuestaSesionEmpleado>(`${environment.apiUrl}/api/empleados/sesion`, { id, contrasena })
      .pipe(
        tap((r) => guardar(CLAVE_TOKEN_EMPLEADO, r.token)),
        map(() => undefined),
      );
  }

  tokenSistema(): string | null {
    return leer(CLAVE_TOKEN_SISTEMA);
  }

  tokenEmpleado(): string | null {
    return leer(CLAVE_TOKEN_EMPLEADO);
  }

  rol(): string | null {
    return leer(CLAVE_ROL);
  }

  /** Usuario del sistema con sesión, para la barra superior (diseño §3). */
  usuario(): string | null {
    return leer(CLAVE_USUARIO);
  }

  /** Ruta de inicio del rol, entregada por el servidor al iniciar sesión (/punto/{id} o /panel). */
  inicio(): string | null {
    return leer(CLAVE_INICIO);
  }

  /** Hora local (hh:mm) en que termina la sesión del sistema, o null si se desconoce. */
  finSesion(): string | null {
    const crudo = leer(CLAVE_EXPIRA);
    const fecha = crudo ? new Date(crudo) : null;
    if (!fecha || Number.isNaN(fecha.getTime())) {
      return null;
    }
    const dos = (n: number) => String(n).padStart(2, '0');
    return `${dos(fecha.getHours())}:${dos(fecha.getMinutes())}`;
  }

  cerrarSesionEmpleado(): void {
    borrar(CLAVE_TOKEN_EMPLEADO);
  }

  /** Borra solo la sesión del sistema (chef o administrador); conserva la del empleado. */
  cerrarSesionSistema(): void {
    borrar(CLAVE_TOKEN_SISTEMA);
    borrar(CLAVE_ROL);
    borrar(CLAVE_USUARIO);
    borrar(CLAVE_INICIO);
    borrar(CLAVE_EXPIRA);
  }

  cerrarSesion(): void {
    this.cerrarSesionSistema();
    borrar(CLAVE_TOKEN_EMPLEADO);
  }
}
