import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterStateSnapshot, ActivatedRouteSnapshot, UrlTree } from '@angular/router';
import { environment } from '../../environments/environment';
import { borrar, guardar, leer } from './almacen';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { rolGuard } from './rol.guard';

describe('núcleo del frontend', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
      ],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('login_guardaTokenYRol', () => {
    const http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);

    auth.login('chef', 'secreta').subscribe();
    const req = http.expectOne(`${environment.apiUrl}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ usuario: 'chef', contrasena: 'secreta' });
    req.flush({ token: 'tok-sistema', rol: 'Encargado', expiraEn: '2026-11-05T00:00:00' });

    expect(localStorage.getItem('comedor.token.sistema')).toBe('tok-sistema');
    expect(localStorage.getItem('comedor.rol')).toBe('Encargado');
    expect(auth.tokenSistema()).toBe('tok-sistema');
    expect(auth.rol()).toBe('Encargado');
  });

  it('interceptor_usaTokenEmpleadoEnAccesoCelular', () => {
    const http = TestBed.inject(HttpTestingController);
    localStorage.setItem('comedor.token.empleado', 'tok-empleado');
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');

    TestBed.inject(HttpClient).post(`${environment.apiUrl}/api/accesos/celular`, {}).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/api/accesos/celular`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-empleado');
    req.flush({});
  });

  it('interceptor_usaTokenSistemaEnReportes', () => {
    const http = TestBed.inject(HttpTestingController);
    localStorage.setItem('comedor.token.empleado', 'tok-empleado');
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');

    TestBed.inject(HttpClient).get(`${environment.apiUrl}/api/reportes/cotejo`).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/api/reportes/cotejo`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-sistema');
    req.flush([]);
  });

  it('interceptor_noAgregaTokenEnLogin', () => {
    const http = TestBed.inject(HttpTestingController);
    localStorage.setItem('comedor.token.empleado', 'tok-empleado');
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');
    const cliente = TestBed.inject(HttpClient);

    cliente.post(`${environment.apiUrl}/api/auth/login`, {}).subscribe();
    cliente.post(`${environment.apiUrl}/api/empleados/sesion`, {}).subscribe();

    const login = http.expectOne(`${environment.apiUrl}/api/auth/login`);
    const sesion = http.expectOne(`${environment.apiUrl}/api/empleados/sesion`);
    expect(login.request.headers.has('Authorization')).toBeFalse();
    expect(sesion.request.headers.has('Authorization')).toBeFalse();
    login.flush({});
    sesion.flush({});
  });

  it('almacen_noLanzaSiLocalStorageFalla', () => {
    spyOn(Storage.prototype, 'getItem').and.throwError('bloqueado');
    spyOn(Storage.prototype, 'setItem').and.throwError('bloqueado');
    spyOn(Storage.prototype, 'removeItem').and.throwError('bloqueado');

    expect(() => guardar('k', 'v')).not.toThrow();
    expect(leer('k')).toBeNull();
    expect(() => borrar('k')).not.toThrow();
  });

  it('rolGuard_redirigeSinRol', () => {
    const guard = rolGuard(['Administrador']);
    const estado = { url: '/admin' } as RouterStateSnapshot;
    const ruta = {} as ActivatedRouteSnapshot;
    const ejecutar = () =>
      TestBed.runInInjectionContext(() => guard(ruta, estado)) as boolean | UrlTree;

    const sinRol = ejecutar();
    expect(sinRol instanceof UrlTree).toBeTrue();
    expect(TestBed.inject(Router).serializeUrl(sinRol as UrlTree)).toBe('/login?returnUrl=%2Fadmin');

    localStorage.setItem('comedor.rol', 'Supervisor');
    expect(ejecutar() instanceof UrlTree).toBeTrue();

    localStorage.setItem('comedor.rol', 'Administrador');
    expect(ejecutar()).toBeTrue();
  });
});
