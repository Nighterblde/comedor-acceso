import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, Type } from '@angular/core';
import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { QRCodeComponent } from 'angularx-qrcode';
import { of, throwError } from 'rxjs';
import { SincronizacionService } from '../compartido/sincronizacion.service';
import { environment } from '../../environments/environment';
import { AdminService } from './admin.service';
import { ContingenciaComponent } from './contingencia.component';
import { PuntosComponent } from './puntos.component';
import { ReglasComponent } from './reglas.component';
import { SincronizacionComponent } from './sincronizacion.component';
import { UsuariosComponent } from './usuarios.component';

const METODOS: (keyof AdminService)[] = [
  'reglas',
  'guardarRegla',
  'eliminarRegla',
  'puntos',
  'guardarPunto',
  'qrPunto',
  'usuarios',
  'guardarUsuario',
  'sincronizar',
  'bitacora',
  'contingencia',
];

function servicioFalso(): jasmine.SpyObj<AdminService> {
  const s = jasmine.createSpyObj<AdminService>('AdminService', METODOS);
  s.reglas.and.returnValue(of([]));
  s.puntos.and.returnValue(of([{ id: 'P1', nombre: 'Entrada principal', activo: true }]));
  s.usuarios.and.returnValue(of([]));
  s.bitacora.and.returnValue(of([]));
  return s;
}

function crear<T>(
  tipo: Type<T>,
  servicio: jasmine.SpyObj<AdminService>,
  sincronizacion: unknown = { estado: signal(null), refrescar: jasmine.createSpy('refrescar') },
): ComponentFixture<T> {
  TestBed.configureTestingModule({
    declarations: [tipo],
    imports: [FormsModule, QRCodeComponent],
    providers: [
      { provide: AdminService, useValue: servicio },
      { provide: SincronizacionService, useValue: sincronizacion },
    ],
  });
  const f = TestBed.createComponent(tipo);
  f.detectChanges();
  return f;
}

function escribir(f: ComponentFixture<unknown>, selector: string, valor: string): void {
  const campo = f.nativeElement.querySelector(selector) as HTMLInputElement;
  campo.value = valor;
  campo.dispatchEvent(new Event('input'));
  f.detectChanges();
}

function errorApi(status: number, texto: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { error: texto } });
}

describe('módulo admin', () => {
  describe('AdminService', () => {
    it('contingencia_enviaCuerpoAlEndpoint', () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
      const http = TestBed.inject(HttpTestingController);
      const cuerpo = {
        matricula: 'E1001',
        fechaServicio: '2026-11-10',
        tiempoComida: 'Comida',
        referenciaLista: 'Lista 7',
      };

      TestBed.inject(AdminService).contingencia(cuerpo).subscribe();

      const req = http.expectOne(`${environment.apiUrl}/api/admin/contingencia`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(cuerpo);
      req.flush({});
      http.verify();
    });
  });

  describe('reglas', () => {
    it('regla_inicioIgualFin_muestraError', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.guardarRegla.and.returnValue(
        throwError(() => errorApi(400, 'El inicio y el fin no pueden ser iguales')),
      );
      const f = crear(ReglasComponent, servicio);
      escribir(f, '#regla-turno', 'Matutino');
      escribir(f, '#regla-tiempo', 'Comida');
      escribir(f, '#regla-inicio', '13:00');
      escribir(f, '#regla-fin', '13:00');
      (f.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
      tick();
      f.detectChanges();

      expect(servicio.guardarRegla).toHaveBeenCalledTimes(1);
      expect(f.nativeElement.textContent).toContain('El inicio y el fin no pueden ser iguales');
    }));
  });

  describe('puntos', () => {
    it('puntos_imprimirQr_muestraCodigoDelPunto', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.qrPunto.and.returnValue(
        of({ token: 'tok', url: 'http://comedor.local/pase?t=tok' }),
      );
      const f = crear(PuntosComponent, servicio);
      const imprimir = Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button')).find((b) =>
        (b.textContent ?? '').includes('Imprimir QR'),
      )!;
      imprimir.click();
      tick();
      f.detectChanges();

      expect(servicio.qrPunto).toHaveBeenCalledOnceWith('P1');
      const qr = f.debugElement.query((d) => d.componentInstance instanceof QRCodeComponent);
      expect((qr.componentInstance as QRCodeComponent).qrdata).toBe('http://comedor.local/pase?t=tok');
      expect(f.nativeElement.querySelector('.vista-qr')?.textContent).toContain('Entrada principal');
    }));
  });

  describe('sincronizacion', () => {
    it('sincronizar_muestraResultado', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.sincronizar.and.returnValue(
        of({ exito: true, altas: 3, cambios: 2, bajas: 1, omitidos: 4, error: null }),
      );
      const f = crear(SincronizacionComponent, servicio);
      const boton = Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button')).find((b) =>
        (b.textContent ?? '').includes('Sincronizar ahora'),
      )!;
      boton.click();
      tick();
      f.detectChanges();

      const resultado = (f.nativeElement.querySelector('.resultado') as HTMLElement).textContent ?? '';
      expect(resultado).toContain('Altas: 3');
      expect(resultado).toContain('Cambios: 2');
      expect(resultado).toContain('Bajas: 1');
      expect(resultado).toContain('Omitidos: 4');
      expect(servicio.bitacora).toHaveBeenCalledTimes(2);
    }));
  });

  describe('sincronizacion y aviso', () => {
    it('sincronizar_actualizaAviso', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.sincronizar.and.returnValue(
        of({ exito: true, altas: 0, cambios: 0, bajas: 0, omitidos: 0, error: null }),
      );
      const sincronizacion = { estado: signal(null), refrescar: jasmine.createSpy('refrescar') };
      const f = crear(SincronizacionComponent, servicio, sincronizacion);
      Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button'))
        .find((b) => (b.textContent ?? '').includes('Sincronizar ahora'))!
        .click();
      tick();

      expect(sincronizacion.refrescar).toHaveBeenCalledTimes(1);
    }));
  });

  describe('claves al editar', () => {
    function editarPrimero(f: ComponentFixture<unknown>): void {
      Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button'))
        .find((b) => (b.textContent ?? '').includes('Editar'))!
        .click();
      f.detectChanges();
    }

    it('puntos_editar_claveNoEditable', fakeAsync(() => {
      const f = crear(PuntosComponent, servicioFalso());
      tick();
      expect((f.nativeElement.querySelector('#punto-id') as HTMLInputElement).readOnly).toBeFalse();

      editarPrimero(f);
      tick();
      f.detectChanges();

      expect((f.nativeElement.querySelector('#punto-id') as HTMLInputElement).readOnly).toBeTrue();
    }));

    it('usuarios_editar_usuarioNoEditable', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.usuarios.and.returnValue(of([{ usuario: 'chef1', rol: 'Encargado', activo: true }]));
      const f = crear(UsuariosComponent, servicio);
      tick();
      f.detectChanges();
      expect((f.nativeElement.querySelector('#usuario-nombre') as HTMLInputElement).readOnly).toBeFalse();

      editarPrimero(f);
      tick();
      f.detectChanges();

      expect((f.nativeElement.querySelector('#usuario-nombre') as HTMLInputElement).readOnly).toBeTrue();
    }));
  });

  describe('contingencia', () => {
    function llenar(f: ComponentFixture<ContingenciaComponent>, referencia: string) {
      escribir(f, '#cont-matricula', 'E1001');
      escribir(f, '#cont-fecha', '2026-11-10');
      escribir(f, '#cont-tiempo', 'Comida');
      escribir(f, '#cont-referencia', referencia);
    }

    it('contingencia_sinReferencia_noEnvia', fakeAsync(() => {
      const servicio = servicioFalso();
      const f = crear(ContingenciaComponent, servicio);
      tick();
      llenar(f, '   ');
      (f.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
      tick();

      expect(servicio.contingencia).not.toHaveBeenCalled();
      expect((f.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBeTrue();
    }));

    it('contingencia_409_muestraYaConsumiste', fakeAsync(() => {
      const servicio = servicioFalso();
      servicio.contingencia.and.returnValue(
        throwError(() => errorApi(409, 'Ya consumiste este tiempo de comida')),
      );
      const f = crear(ContingenciaComponent, servicio);
      tick();
      llenar(f, 'Lista 7');
      (f.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
      tick();
      f.detectChanges();

      expect(servicio.contingencia).toHaveBeenCalledOnceWith({
        matricula: 'E1001',
        fechaServicio: '2026-11-10',
        tiempoComida: 'Comida',
        referenciaLista: 'Lista 7',
      });
      expect(f.nativeElement.textContent).toContain('Ya consumiste este tiempo de comida');
    }));
  });
});
