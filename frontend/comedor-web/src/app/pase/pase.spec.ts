import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { ResultadoAcceso } from '../modelos/pase.model';
import { PaseComponent } from './pase.component';
import { PaseModule } from './pase.module';

const URL_CELULAR = `${environment.apiUrl}/api/accesos/celular`;
const URL_SESION = `${environment.apiUrl}/api/empleados/sesion`;
const TEXTO_ESCANEAR = 'Escanea de nuevo el código de la entrada';

function resultado(parcial: Partial<ResultadoAcceso>): ResultadoAcceso {
  return {
    estado: 'Autorizado',
    motivo: null,
    matricula: '12345',
    nombre: 'Ana López Pérez',
    turno: 'Matutino',
    tiempoComida: 'Desayuno',
    fotoUrl: null,
    hora: '2026-11-04T08:15:30',
    ...parcial,
  };
}

describe('módulo Pase', () => {
  let http: HttpTestingController;

  function crear(t: string | null, registro = false): ComponentFixture<PaseComponent> {
    TestBed.configureTestingModule({
      imports: [PaseModule],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(t ? { t } : {}),
              data: registro ? { registro: true } : {},
            },
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(PaseComponent);
    fixture.detectChanges();
    return fixture;
  }

  const texto = (f: ComponentFixture<PaseComponent>): string => f.nativeElement.textContent ?? '';

  beforeEach(() => localStorage.clear());

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('sinSesion_muestraLogin', () => {
    const f = crear('tok.mac');

    expect(f.nativeElement.querySelector('app-login-empleado')).not.toBeNull();
    expect(f.nativeElement.querySelector('#id')).not.toBeNull();
    expect(f.nativeElement.querySelector('#contrasena')).not.toBeNull();
    http.expectNone(URL_CELULAR);
  });

  it('conSesion_llamaValidarConToken', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    crear('abc.def');

    const req = http.expectOne(URL_CELULAR);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ tokenQr: 'abc.def' });
    req.flush(resultado({}));
  });

  it('autorizado_muestraAutorizadoYNombre', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush(resultado({}));
    f.detectChanges();

    const t = texto(f);
    expect(t).toContain('AUTORIZADO');
    expect(t).toContain('Pasa con el chef');
    expect(t).toContain('Ana López Pérez');
    expect(t).toContain('12345');
    expect(t).toContain('Matutino');
    expect(t).toContain('Desayuno');
    expect(f.nativeElement.querySelector('[aria-label="Autorizado"]')).not.toBeNull();
  });

  it('rechazado_muestraMotivo', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http
      .expectOne(URL_CELULAR)
      .flush(resultado({ estado: 'Rechazado', motivo: 'Fuera de horario de tu turno' }));
    f.detectChanges();

    const t = texto(f);
    expect(t).toContain('RECHAZADO');
    expect(t).toContain('Fuera de horario de tu turno');
    expect(t).toContain('08:15:30');
    expect(t).not.toContain('Pasa con el chef');
    expect(f.nativeElement.querySelector('[aria-label="Rechazado"]')).not.toBeNull();
  });

  it('escanearDeNuevo_muestraTextoExacto', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush(resultado({ estado: 'EscanearDeNuevo', nombre: null }));
    f.detectChanges();

    expect(texto(f)).toContain(TEXTO_ESCANEAR);
    expect(texto(f)).not.toContain('AUTORIZADO');
    expect(texto(f)).not.toContain('RECHAZADO');
  });

  it('sinToken_conSesion_muestraEscanearSinValidar', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear(null);

    expect(texto(f)).toContain(TEXTO_ESCANEAR);
    http.expectNone(URL_CELULAR);
  });

  it('relojAvanza', fakeAsync(() => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush(resultado({}));
    f.detectChanges();
    const leer = () => f.nativeElement.querySelector('[data-prueba="reloj"]').textContent.trim();

    const antes = leer();
    tick(1000);
    f.detectChanges();

    expect(leer()).not.toBe(antes);
    f.destroy();
  }));

  it('respuesta401_vuelveALogin', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt-vencido');
    localStorage.setItem('comedor.token.sistema', 'jwt-sistema');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush({}, { status: 401, statusText: 'Unauthorized' });
    f.detectChanges();

    expect(localStorage.getItem('comedor.token.empleado')).toBeNull();
    expect(localStorage.getItem('comedor.token.sistema')).toBe('jwt-sistema');
    expect(f.nativeElement.querySelector('app-login-empleado')).not.toBeNull();

    // Conserva t: tras iniciar sesión de nuevo, vuelve a validar el mismo token.
    const el: HTMLElement = f.nativeElement;
    (el.querySelector('#id') as HTMLInputElement).value = '12345';
    (el.querySelector('#id') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('#contrasena') as HTMLInputElement).value = 'secreta';
    (el.querySelector('#contrasena') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    http.expectOne(URL_SESION).flush({ token: 'jwt-nuevo' });
    const segunda = http.expectOne(URL_CELULAR);
    expect(segunda.request.body).toEqual({ tokenQr: 'abc.def' });
    segunda.flush(resultado({}));
  });

  it('errorDeRedOServidor_muestraEscanearDeNuevo', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush({}, { status: 500, statusText: 'Error' });
    f.detectChanges();

    expect(texto(f)).toContain(TEXTO_ESCANEAR);
    expect(localStorage.getItem('comedor.token.empleado')).toBe('jwt');
  });

  it('rechazadoNoRegistrado_conNulos_muestraMotivoSinEtiquetasVacias', () => {
    localStorage.setItem('comedor.token.empleado', 'jwt');
    const f = crear('abc.def');
    http.expectOne(URL_CELULAR).flush(
      resultado({
        estado: 'Rechazado',
        motivo: 'No registrado',
        nombre: null,
        matricula: null,
        turno: null,
        tiempoComida: null,
        fotoUrl: null,
      }),
    );
    f.detectChanges();

    expect(texto(f)).toContain('RECHAZADO');
    expect(texto(f)).toContain('No registrado');
    expect(texto(f)).not.toContain('Matrícula');
    expect(f.nativeElement.querySelector('.chip')).toBeNull();
    expect(f.nativeElement.querySelector('.avatar')).toBeNull();
  });

  it('login_conCamposVacios_noEnviaYDeshabilitaEntrar', () => {
    const f = crear('abc.def');
    const el: HTMLElement = f.nativeElement;

    expect((el.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBeTrue();
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    http.expectNone(URL_SESION);
  });

  it('login_muestraPuntoDelToken', () => {
    const t = btoa('P1|638000000').replace(/=+$/, '') + '.mac';
    const f = crear(t);

    expect(texto(f)).toContain('Entrada P1');
  });

  it('login_errorBloqueado_muestraMensaje', () => {
    const f = crear('abc.def');
    const el: HTMLElement = f.nativeElement;
    (el.querySelector('#id') as HTMLInputElement).value = '12345';
    (el.querySelector('#id') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('#contrasena') as HTMLInputElement).value = 'x';
    (el.querySelector('#contrasena') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    http.expectOne(URL_SESION).flush({ error: 'Bloqueado' }, { status: 401, statusText: 'Unauthorized' });
    f.detectChanges();

    expect(texto(f)).toContain('Cuenta bloqueada por 15 minutos');
  });

  it('registro_trasLogin_muestraListo_sinValidar', () => {
    const f = crear(null, true);
    const el: HTMLElement = f.nativeElement;
    expect(el.querySelector('app-login-empleado')).not.toBeNull();
    (el.querySelector('#id') as HTMLInputElement).value = '12345';
    (el.querySelector('#id') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('#contrasena') as HTMLInputElement).value = 'secreta';
    (el.querySelector('#contrasena') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    http.expectOne(URL_SESION).flush({ token: 'jwt-nuevo' });
    f.detectChanges();

    expect(texto(f)).toContain('Listo. El día que abra el comedor solo escanea el código de la entrada');
    http.expectNone(URL_CELULAR);
  });
});
