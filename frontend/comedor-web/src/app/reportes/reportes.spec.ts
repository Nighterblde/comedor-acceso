import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import { AvisoSincronizacionComponent } from '../compartido/aviso-sincronizacion.component';
import { SincronizacionService } from '../compartido/sincronizacion.service';
import { AuthService } from '../core/auth.service';
import { EstadoSincronizacion } from '../modelos/reporte.model';
import { ReportesComponent } from './reportes.component';
import { ReportesService } from './reportes.service';

describe('módulo reportes', () => {
  describe('ReportesService', () => {
    let servicio: ReportesService;
    let http: HttpTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting()],
      });
      servicio = TestBed.inject(ReportesService);
      http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('cotejo_enviaRangoComoQuery', () => {
      servicio.cotejo('2026-11-01', '2026-11-15').subscribe();

      const req = http.expectOne((r) => r.url === `${environment.apiUrl}/api/reportes/cotejo`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('desde')).toBe('2026-11-01');
      expect(req.request.params.get('hasta')).toBe('2026-11-15');
      req.flush([]);
    });

    it('detalle_enviaSoloLosFiltrosIndicados', () => {
      servicio.detalle('2026-11-01', '2026-11-15', { canal: 'Manual' }).subscribe();

      const req = http.expectOne((r) => r.url === `${environment.apiUrl}/api/reportes/detalle`);
      expect(req.request.params.get('canal')).toBe('Manual');
      expect(req.request.params.has('matricula')).toBeFalse();
      req.flush([]);
    });

    it('descargar_creaArchivoConNombre', () => {
      spyOn(URL, 'createObjectURL').and.returnValue('blob:prueba');
      spyOn(URL, 'revokeObjectURL');
      let enlace: HTMLAnchorElement | undefined;
      spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) {
        enlace = this;
      });

      servicio
        .descargar('cotejo.xlsx?desde=2026-11-01&hasta=2026-11-15', 'cotejo-2026-11-01-2026-11-15.xlsx')
        .subscribe();

      const req = http.expectOne(
        `${environment.apiUrl}/api/reportes/cotejo.xlsx?desde=2026-11-01&hasta=2026-11-15`,
      );
      expect(req.request.responseType).toBe('blob');
      req.flush(new Blob(['x']));

      expect(enlace?.download).toBe('cotejo-2026-11-01-2026-11-15.xlsx');
      expect(enlace?.href).toBe('blob:prueba');
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:prueba');
    });
  });

  describe('ReportesComponent', () => {
    function armar() {
      const servicio = jasmine.createSpyObj<ReportesService>('ReportesService', [
        'conteo',
        'cotejo',
        'detalle',
        'descargar',
      ]);
      servicio.cotejo.and.returnValue(
        of([
          { fechaServicio: '2026-11-10', tiempoComida: 'Comida', servidas: 2, sinServir: 1 },
          { fechaServicio: '2026-11-10', tiempoComida: 'Cena', servidas: 2, sinServir: 0 },
        ]),
      );
      servicio.descargar.and.returnValue(of(undefined));
      const sincronizacion = { estado: signal(null), refrescar: jasmine.createSpy('refrescar') };
      TestBed.configureTestingModule({
        declarations: [ReportesComponent, AvisoSincronizacionComponent],
        imports: [FormsModule],
        providers: [
          { provide: ReportesService, useValue: servicio },
          { provide: SincronizacionService, useValue: sincronizacion },
          { provide: AuthService, useValue: { rol: () => 'Administrador', usuario: () => 'super', finSesion: () => '14:30' } },
        ],
      });
      const f = TestBed.createComponent(ReportesComponent);
      f.componentInstance.desde = '2026-11-01';
      f.componentInstance.hasta = '2026-11-15';
      f.detectChanges();
      return { f, servicio };
    }

    function boton(f: { nativeElement: HTMLElement }, texto: string): HTMLButtonElement {
      return Array.from(f.nativeElement.querySelectorAll('button')).find((b) =>
        (b.textContent ?? '').includes(texto),
      ) as HTMLButtonElement;
    }

    it('consultarCotejo_muestraFilaTotal', fakeAsync(() => {
      const { f, servicio } = armar();
      boton(f, 'Consultar').click();
      tick();
      f.detectChanges();

      expect(servicio.cotejo).toHaveBeenCalledOnceWith('2026-11-01', '2026-11-15');
      const total = f.nativeElement.querySelector('tr.total') as HTMLElement;
      expect(total.textContent).toContain('Total');
      expect(total.textContent).toContain('4');
      expect(total.textContent).toContain('1');
    }));

    it('exportarExcel_descargaConNombreDelRango', fakeAsync(() => {
      const { f, servicio } = armar();
      boton(f, 'Excel').click();
      tick();

      expect(servicio.descargar).toHaveBeenCalledOnceWith(
        'cotejo.xlsx?desde=2026-11-01&hasta=2026-11-15',
        'cotejo-2026-11-01-2026-11-15.xlsx',
      );
    }));
  });

  describe('ReportesComponent, rango y detalle', () => {
    function armar() {
      const servicio = jasmine.createSpyObj<ReportesService>('ReportesService', [
        'conteo',
        'cotejo',
        'detalle',
        'descargar',
      ]);
      servicio.cotejo.and.returnValue(
        of([{ fechaServicio: '2026-11-10', tiempoComida: 'Comida', servidas: 2, sinServir: 1 }]),
      );
      servicio.descargar.and.returnValue(of(undefined));
      TestBed.configureTestingModule({
        declarations: [ReportesComponent, AvisoSincronizacionComponent],
        imports: [FormsModule],
        providers: [
          { provide: ReportesService, useValue: servicio },
          { provide: SincronizacionService, useValue: { estado: signal(null), refrescar: () => undefined } },
          { provide: AuthService, useValue: { rol: () => 'Administrador', usuario: () => 'super', finSesion: () => '14:30' } },
        ],
      });
      const f = TestBed.createComponent(ReportesComponent);
      f.componentInstance.desde = '2026-11-01';
      f.componentInstance.hasta = '2026-11-15';
      f.detectChanges();
      return { f, servicio };
    }

    function boton(f: { nativeElement: HTMLElement }, texto: string): HTMLButtonElement | undefined {
      return Array.from(f.nativeElement.querySelectorAll('button')).find((b) =>
        (b.textContent ?? '').includes(texto),
      ) as HTMLButtonElement | undefined;
    }

    it('cambiarRango_ocultaResultadosAnteriores', fakeAsync(() => {
      const { f } = armar();
      boton(f, 'Consultar')!.click();
      tick();
      f.detectChanges();
      expect(f.nativeElement.querySelector('tr.total')).not.toBeNull();

      const desde = f.nativeElement.querySelector('#rep-desde') as HTMLInputElement;
      desde.value = '2026-11-02';
      desde.dispatchEvent(new Event('input'));
      tick();
      f.detectChanges();

      expect(f.nativeElement.querySelector('tr.total')).toBeNull();
    }));

    it('exportarDetalle_incluyeFiltros', fakeAsync(() => {
      const { f, servicio } = armar();
      boton(f, 'Detalle')!.click();
      f.componentInstance.filtros = { canal: 'Manual', motivo: 'YaConsumio' };
      f.detectChanges();
      boton(f, 'Excel')!.click();
      tick();

      expect(servicio.descargar).toHaveBeenCalledOnceWith(
        'detalle.xlsx?desde=2026-11-01&hasta=2026-11-15&canal=Manual&motivo=YaConsumio',
        'detalle-2026-11-01-2026-11-15.xlsx',
      );
    }));

    it('detalle_sinBotonPdf', () => {
      const { f } = armar();
      expect(boton(f, 'PDF')).toBeDefined();

      boton(f, 'Detalle')!.click();
      f.detectChanges();

      expect(boton(f, 'PDF')).toBeUndefined();
    });

    it('barra_muestraUsuarioYRol', () => {
      const { f } = armar();
      const barra = (f.nativeElement.querySelector('header') as HTMLElement).textContent ?? '';
      expect(barra).toContain('super');
      expect(barra).toContain('Administrador');
    });
  });

  describe('SincronizacionService', () => {
    it('refrescar_guardaElEstadoDeLaApi', () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
      const http = TestBed.inject(HttpTestingController);
      const servicio = TestBed.inject(SincronizacionService);

      servicio.refrescar();
      http
        .expectOne(`${environment.apiUrl}/api/sincronizacion/estado`)
        .flush({ ultimaFallida: true, ultimaEjecucion: '2026-11-04T02:00:00', ultimaExitosa: null });

      expect(servicio.estado()?.ultimaFallida).toBeTrue();
      http.verify();
    });
  });

  describe('AvisoSincronizacionComponent', () => {
    function armar(estado: EstadoSincronizacion | null) {
      const sincronizacion = { estado: signal(estado), refrescar: jasmine.createSpy('refrescar') };
      TestBed.configureTestingModule({
        declarations: [AvisoSincronizacionComponent],
        providers: [{ provide: SincronizacionService, useValue: sincronizacion }],
      });
      const f = TestBed.createComponent(AvisoSincronizacionComponent);
      f.detectChanges();
      return { f, sincronizacion };
    }

    it('ultimaSincronizacionFallida_muestraAviso', () => {
      const { f, sincronizacion } = armar({
        ultimaFallida: true,
        ultimaEjecucion: '2026-11-04T02:00:00',
        ultimaExitosa: '2026-11-03T02:00:00',
      });

      expect(sincronizacion.refrescar).toHaveBeenCalledTimes(1);
      const aviso = f.nativeElement.querySelector('[role="status"]') as HTMLElement;
      expect(aviso.textContent).toContain(
        'La última sincronización con RR. HH. falló; se usan los datos del 03/11/2026',
      );
    });

    it('ultimaSincronizacionExitosa_noMuestraAviso', () => {
      const { f } = armar({
        ultimaFallida: false,
        ultimaEjecucion: '2026-11-04T02:00:00',
        ultimaExitosa: '2026-11-04T02:00:00',
      });

      expect(f.nativeElement.querySelector('[role="status"]')).toBeNull();
    });

    it('aviso_seActualizaAlCambiarElEstado', () => {
      const { f, sincronizacion } = armar({
        ultimaFallida: true,
        ultimaEjecucion: '2026-11-04T02:00:00',
        ultimaExitosa: '2026-11-03T02:00:00',
      });

      sincronizacion.estado.set({
        ultimaFallida: false,
        ultimaEjecucion: '2026-11-05T10:00:00',
        ultimaExitosa: '2026-11-05T10:00:00',
      });
      f.detectChanges();

      expect(f.nativeElement.querySelector('[role="status"]')).toBeNull();
    });
  });
});
