import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterStateSnapshot, ActivatedRouteSnapshot, UrlTree } from '@angular/router';
import { environment } from '../../environments/environment';
import { borrar, guardar, leer } from './almacen';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { inicioGuard } from './inicio.guard';
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
    req.flush({ token: 'tok-sistema', rol: 'Chef', expiraEn: '2026-11-05T00:00:00Z', inicio: '/punto/P1' });

    expect(localStorage.getItem('comedor.token.sistema')).toBe('tok-sistema');
    expect(localStorage.getItem('comedor.rol')).toBe('Chef');
    expect(auth.tokenSistema()).toBe('tok-sistema');
    expect(auth.rol()).toBe('Chef');
  });

  it('login_guardaUsuario_yCerrarSesionLoBorra', () => {
    const http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);

    auth.login(' chef ', 'secreta').subscribe();
    http
      .expectOne(`${environment.apiUrl}/api/auth/login`)
      .flush({ token: 'tok-sistema', rol: 'Chef', expiraEn: '2026-11-05T00:00:00' });

    expect(auth.usuario()).toBe('chef');
    auth.cerrarSesion();
    expect(auth.usuario()).toBeNull();
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

  it('interceptor_usaTokenEmpleadoEnMiTurno', () => {
    const http = TestBed.inject(HttpTestingController);
    localStorage.setItem('comedor.token.empleado', 'tok-empleado');
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');

    TestBed.inject(HttpClient).get(`${environment.apiUrl}/api/accesos/mi-turno`).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/api/accesos/mi-turno`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-empleado');
    req.flush({});
  });

  it('respuesta401_deMiTurno_noBorraSesionDelSistemaNiNavega', () => {
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navegar = spyOn(router, 'navigate').and.resolveTo(true);
    localStorage.setItem('comedor.token.empleado', 'tok-empleado');
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');

    TestBed.inject(HttpClient).get(`${environment.apiUrl}/api/accesos/mi-turno`).subscribe({ error: () => {} });
    http.expectOne(`${environment.apiUrl}/api/accesos/mi-turno`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('comedor.token.sistema')).toBe('tok-sistema');
    expect(navegar).not.toHaveBeenCalled();
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

    localStorage.setItem('comedor.rol', 'Chef');
    expect(ejecutar() instanceof UrlTree).toBeTrue();

    localStorage.setItem('comedor.rol', 'Administrador');
    expect(ejecutar()).toBeTrue();
  });

  it('rutaInicio_conSesion_vaAlInicioGuardado', () => {
    localStorage.setItem('comedor.token.sistema', 'tok');
    localStorage.setItem('comedor.inicio', '/punto/P1');
    const r = TestBed.runInInjectionContext(() =>
      inicioGuard({} as ActivatedRouteSnapshot, { url: '/inicio' } as RouterStateSnapshot),
    ) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(r)).toBe('/punto/P1');
  });

  it('rutaInicio_sinSesion_vaALogin', () => {
    const r = TestBed.runInInjectionContext(() =>
      inicioGuard({} as ActivatedRouteSnapshot, { url: '/inicio' } as RouterStateSnapshot),
    ) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(r)).toBe('/login?returnUrl=%2Finicio');
  });

  it('respuesta401_borraSesionYVaALogin', () => {
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    spyOnProperty(router, 'url').and.returnValue('/punto/P1');
    const navegar = spyOn(router, 'navigate').and.resolveTo(true);
    localStorage.setItem('comedor.token.sistema', 'tok-sistema');
    localStorage.setItem('comedor.rol', 'Chef');
    localStorage.setItem('comedor.inicio', '/punto/P1');

    TestBed.inject(HttpClient).get(`${environment.apiUrl}/api/puntos/P1/solicitudes`).subscribe({ error: () => {} });
    http
      .expectOne(`${environment.apiUrl}/api/puntos/P1/solicitudes`)
      .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('comedor.token.sistema')).toBeNull();
    expect(localStorage.getItem('comedor.inicio')).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/punto/P1' },
      state: { aviso: 'Tu sesión terminó; vuelve a iniciar sesión' },
    });
  });

  it('respuesta401_deLoginNoNavega', () => {
    const http = TestBed.inject(HttpTestingController);
    const navegar = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);

    TestBed.inject(HttpClient).post(`${environment.apiUrl}/api/auth/login`, {}).subscribe({ error: () => {} });
    http.expectOne(`${environment.apiUrl}/api/auth/login`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(navegar).not.toHaveBeenCalled();
  });

  it('finSesion_formateaHoraLocal', () => {
    localStorage.setItem('comedor.expira', new Date(2026, 10, 10, 9, 5).toISOString());

    expect(TestBed.inject(AuthService).finSesion()).toBe('09:05');
  });
});
