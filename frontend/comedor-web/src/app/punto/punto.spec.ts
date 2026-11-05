import { Type } from '@angular/core';
import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ResultadoAcceso } from '../modelos/pase.model';
import { SolicitudPunto } from '../modelos/consumo.model';
import { CapturaManualComponent } from './captura-manual.component';
import { PuntoService } from './punto.service';
import { SolicitudesComponent } from './solicitudes.component';

function solicitud(parcial: Partial<SolicitudPunto>): SolicitudPunto {
  return {
    consumoId: 'c1',
    fechaHora: '2026-11-04T12:00:00',
    matricula: 'A1',
    nombre: 'Ana Pérez',
    fotoUrl: null,
    turno: 'Matutino',
    tiempoComida: 'Comida',
    fechaServicio: '2026-11-04',
    autorizado: true,
    motivo: null,
    servido: false,
    ...parcial,
  };
}

function crear<T>(tipo: Type<T>, servicio: unknown): ComponentFixture<T> {
  TestBed.configureTestingModule({
    declarations: [tipo],
    imports: [FormsModule],
    providers: [{ provide: PuntoService, useValue: servicio }],
  });
  const f = TestBed.createComponent(tipo);
  (f.componentInstance as { puntoId: string }).puntoId = 'P1';
  return f;
}

describe('módulo punto', () => {
  describe('solicitudes', () => {
    function armar(lista: SolicitudPunto[]) {
      const servicio = jasmine.createSpyObj<PuntoService>('PuntoService', ['solicitudes', 'marcarServido']);
      servicio.solicitudes.and.returnValue(of(lista));
      servicio.marcarServido.and.returnValue(of({ yaServido: false }));
      const f = crear(SolicitudesComponent, servicio);
      f.detectChanges();
      tick(0);
      f.detectChanges();
      return { f, servicio };
    }

    it('solicitudes_consultaCada2Segundos', fakeAsync(() => {
      const { f, servicio } = armar([]);
      expect(servicio.solicitudes).toHaveBeenCalledTimes(1);
      expect(servicio.solicitudes.calls.mostRecent().args[0]).toBe('P1');
      tick(2000);
      expect(servicio.solicitudes).toHaveBeenCalledTimes(2);
      tick(4000);
      expect(servicio.solicitudes).toHaveBeenCalledTimes(4);
      f.destroy();
    }));

    it('autorizadoSinServir_muestraBotonServido', fakeAsync(() => {
      const { f } = armar([solicitud({})]);
      const botones = Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button'));
      expect(botones.length).toBe(1);
      expect(botones[0].textContent).toContain('Servido');
      expect(f.nativeElement.textContent).toContain('Ana Pérez');
      f.destroy();
    }));

    it('tarjeta_muestraNombreSinFoto', fakeAsync(() => {
      const { f } = armar([solicitud({ fotoUrl: 'http://x/foto.jpg' })]);
      const texto: string = f.nativeElement.textContent;
      expect(texto).toContain('Ana Pérez');
      expect(texto).toContain('Matutino');
      expect(texto).toContain('Comida');
      expect(f.nativeElement.querySelector('img')).toBeNull();
      expect(f.nativeElement.querySelector('.avatar')).toBeNull();
      f.destroy();
    }));

    it('errorDeRed_muestraSinConexion', fakeAsync(() => {
      const servicio = jasmine.createSpyObj<PuntoService>('PuntoService', ['solicitudes', 'marcarServido']);
      servicio.solicitudes.and.returnValues(
        throwError(() => new Error('red')),
        of([solicitud({})]),
      );
      const f = crear(SolicitudesComponent, servicio);
      f.detectChanges();
      tick(0);
      f.detectChanges();
      expect(f.nativeElement.textContent).toContain('Sin conexión');

      tick(2000);
      f.detectChanges();
      expect(servicio.solicitudes).toHaveBeenCalledTimes(2);
      expect(f.nativeElement.textContent).not.toContain('Sin conexión');
      expect(f.nativeElement.textContent).toContain('Ana Pérez');
      f.destroy();
    }));

    it('rechazado_noMuestraBotones', fakeAsync(() => {
      const { f } = armar([
        solicitud({ consumoId: null, autorizado: false, motivo: 'Fuera de horario' }),
      ]);
      expect(f.nativeElement.querySelectorAll('button').length).toBe(0);
      expect(f.nativeElement.textContent).toContain('Fuera de horario');
      f.destroy();
    }));

    it('servido_quitaTarjetaDeLaLista', fakeAsync(() => {
      const { f, servicio } = armar([solicitud({})]);
      f.nativeElement.querySelector('button').click();
      f.detectChanges();

      expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
      expect(f.nativeElement.querySelectorAll('button').length).toBe(0);
      f.destroy();
    }));

    it('noHayBotonesDeAprobarNiRechazar', fakeAsync(() => {
      const { f } = armar([
        solicitud({}),
        solicitud({ consumoId: null, autorizado: false, motivo: 'Fuera de horario' }),
      ]);
      const textos = Array.from<HTMLButtonElement>(f.nativeElement.querySelectorAll('button')).map((b) =>
        (b.textContent ?? '').toLowerCase(),
      );
      expect(textos.some((t) => t.includes('aprobar') || t.includes('rechazar') || t.includes('autorizar'))).toBeFalse();
      f.destroy();
    }));
  });

  describe('captura-manual', () => {
    it('enter_enviaMatricula', fakeAsync(() => {
      const servicio = jasmine.createSpyObj<PuntoService>('PuntoService', ['capturaManual']);
      servicio.capturaManual.and.returnValue(of({} as ResultadoAcceso));
      const f = crear(CapturaManualComponent, servicio);
      f.detectChanges();
      tick();

      const input: HTMLInputElement = f.nativeElement.querySelector('input');
      input.value = 'A123';
      input.dispatchEvent(new Event('input'));
      f.detectChanges();
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      tick();
      f.detectChanges();

      tick();
      expect(servicio.capturaManual).toHaveBeenCalledOnceWith('A123', 'P1');
      expect(input.value).toBe('');
      f.destroy();
    }));
  });
});
