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

  cerrarSesionEmpleado(): void {
    borrar(CLAVE_TOKEN_EMPLEADO);
  }

  cerrarSesion(): void {
    borrar(CLAVE_TOKEN_SISTEMA);
    borrar(CLAVE_TOKEN_EMPLEADO);
    borrar(CLAVE_ROL);
    borrar(CLAVE_USUARIO);
  }
}
