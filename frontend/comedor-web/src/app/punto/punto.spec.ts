import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { FilaServicio, SolicitudPunto } from '../modelos/consumo.model';
import { FilaComponent } from './fila.component';
import { PuntoService } from './punto.service';

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

const vacia: FilaServicio = { actual: null, siguientes: [], total: 0 };
const conAna: FilaServicio = { actual: solicitud({}), siguientes: [], total: 1 };

function crearServicio() {
  return jasmine.createSpyObj<PuntoService>('PuntoService', ['fila', 'marcarServido']);
}

function montar(servicio: jasmine.SpyObj<PuntoService>, modo: 'tv' | 'panel' = 'tv') {
  TestBed.configureTestingModule({
    declarations: [FilaComponent],
    providers: [{ provide: PuntoService, useValue: servicio }],
  });
  const f: ComponentFixture<FilaComponent> = TestBed.createComponent(FilaComponent);
  f.componentInstance.puntoId = 'P1';
  f.componentInstance.modo = modo;
  f.detectChanges();
  tick(0);
  f.detectChanges();
  return f;
}

function armar(fila: FilaServicio, modo: 'tv' | 'panel' = 'tv') {
  const servicio = crearServicio();
  servicio.fila.and.returnValue(of(fila));
  servicio.marcarServido.and.returnValue(of({ yaServido: false }));
  return { f: montar(servicio, modo), servicio };
}

function tecla(init: KeyboardEventInit = { key: 'a' }) {
  document.dispatchEvent(new KeyboardEvent('keydown', init));
}

describe('pantalla del chef (fila)', () => {
  it('conActual_muestraSirveleAYNombre', fakeAsync(() => {
    const { f } = armar(conAna);
    const texto: string = f.nativeElement.textContent;
    expect(texto).toContain('Sírvele a');
    expect(texto).toContain('Ana Pérez');
    expect(texto).toContain('Matutino');
    expect(texto).toContain('Comida');
    expect(texto).not.toContain('Esperando al siguiente…');
    f.destroy();
  }));

  it('sinActual_muestraEsperando', fakeAsync(() => {
    const { f } = armar(vacia);
    expect(f.nativeElement.textContent).toContain('Esperando al siguiente…');
    expect(f.nativeElement.textContent).not.toContain('Sírvele a');
    f.destroy();
  }));

  it('muestraSiguientesYTotal', fakeAsync(() => {
    const { f } = armar({
      actual: solicitud({}),
      siguientes: [
        solicitud({ consumoId: 'c2', nombre: 'Beto Ruiz' }),
        solicitud({ consumoId: 'c3', nombre: 'Carla Soto' }),
      ],
      total: 5,
    });
    const texto: string = f.nativeElement.textContent;
    expect(texto).toContain('Siguen');
    expect(texto).toContain('Beto Ruiz');
    expect(texto).toContain('Carla Soto');
    expect(texto).toContain('4 en la fila');
    f.destroy();
  }));

  it('nombre_seMuestraA96pxOMas', fakeAsync(() => {
    const { f } = armar(conAna);
    document.body.appendChild(f.nativeElement);
    const nombre: HTMLElement = f.nativeElement.querySelector('.nombre');
    expect(parseFloat(getComputedStyle(nombre).fontSize)).toBeGreaterThanOrEqual(96);
    f.nativeElement.remove();
    f.destroy();
  }));

  it('teclaCualquiera_marcaServidoDelActual', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    tecla({ key: 'q' });
    expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
    tick(1500);
    f.destroy();
  }));

  it('marcar_vuelveAPedirLaFila', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    expect(servicio.fila).toHaveBeenCalledTimes(1);
    tecla();
    expect(servicio.fila).toHaveBeenCalledTimes(2);
    tick(1500);
    f.destroy();
  }));

  it('clic_marcaServido', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    document.dispatchEvent(new MouseEvent('click'));
    expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
    tick(1500);
    f.destroy();
  }));

  it('toque_marcaServido', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    document.dispatchEvent(new Event('touchstart'));
    expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
    tick(1500);
    f.destroy();
  }));

  it('sinActual_teclaNoEnviaNada', fakeAsync(() => {
    const { f, servicio } = armar(vacia);
    tecla();
    document.dispatchEvent(new MouseEvent('click'));
    expect(servicio.marcarServido).not.toHaveBeenCalled();
    f.destroy();
  }));

  it('dosTeclasSeguidas_unaSolaPeticion', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    tecla();
    tecla();
    document.dispatchEvent(new MouseEvent('click'));
    document.dispatchEvent(new Event('touchstart'));
    tick(500);
    tecla();
    expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
    tick(1500);
    f.destroy();
  }));

  it('peticionEnCurso_ignoraEntradaAunPasados1500ms', fakeAsync(() => {
    const servicio = crearServicio();
    servicio.fila.and.returnValue(of(conAna));
    servicio.marcarServido.and.returnValue(new Subject<{ yaServido: boolean }>());
    const f = montar(servicio);
    tecla();
    tick(3000);
    tecla();
    expect(servicio.marcarServido).toHaveBeenCalledTimes(1);
    f.destroy();
  }));

  it('teclaPasados1500ms_marcaAlSiguiente', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    tecla();
    tick(1499);
    tecla();
    expect(servicio.marcarServido).toHaveBeenCalledTimes(1);

    servicio.fila.and.returnValue(
      of({ actual: solicitud({ consumoId: 'c2', nombre: 'Beto Ruiz' }), siguientes: [], total: 1 }),
    );
    tick(1);
    tick(2000);
    f.detectChanges();
    tecla();
    expect(servicio.marcarServido).toHaveBeenCalledTimes(2);
    expect(servicio.marcarServido.calls.mostRecent().args[0]).toBe('c2');
    tick(1500);
    f.destroy();
  }));

  it('teclaRepetida_ignorada', fakeAsync(() => {
    const { f, servicio } = armar(conAna);
    tecla({ key: 'a', repeat: true });
    expect(servicio.marcarServido).not.toHaveBeenCalled();
    tecla({ key: 'a' });
    tick(1500);
    tecla({ key: 'a', repeat: true });
    expect(servicio.marcarServido).toHaveBeenCalledTimes(1);
    f.destroy();
  }));

  it('consultaCada2Segundos', fakeAsync(() => {
    const { f, servicio } = armar(vacia);
    expect(servicio.fila).toHaveBeenCalledTimes(1);
    expect(servicio.fila.calls.mostRecent().args[0]).toBe('P1');
    tick(2000);
    expect(servicio.fila).toHaveBeenCalledTimes(2);
    tick(4000);
    expect(servicio.fila).toHaveBeenCalledTimes(4);
    f.destroy();
  }));

  it('errorDeRed_muestraSinConexion', fakeAsync(() => {
    const servicio = crearServicio();
    servicio.fila.and.returnValues(throwError(() => new Error('red')), of(conAna));
    const f = montar(servicio);
    expect(f.nativeElement.textContent).toContain('Sin conexión');

    tick(2000);
    f.detectChanges();
    expect(servicio.fila).toHaveBeenCalledTimes(2);
    expect(f.nativeElement.textContent).not.toContain('Sin conexión');
    expect(f.nativeElement.textContent).toContain('Ana Pérez');
    f.destroy();
  }));

  it('noHayCamposNiBotones', fakeAsync(() => {
    const { f } = armar(conAna);
    expect(f.nativeElement.querySelectorAll('button, input, textarea, select').length).toBe(0);
    f.destroy();
  }));

  describe('modo panel', () => {
    it('panel_muestraBotonMarcarServido', fakeAsync(() => {
      const { f } = armar(conAna, 'panel');
      const botones = f.nativeElement.querySelectorAll('button');
      expect(botones.length).toBe(1);
      expect(botones[0].textContent).toContain('Marcar servido');
      f.destroy();
    }));

    it('panel_sinActual_noMuestraBoton', fakeAsync(() => {
      const { f } = armar(vacia, 'panel');
      expect(f.nativeElement.querySelectorAll('button').length).toBe(0);
      f.destroy();
    }));

    it('panel_ignoraTeclasClicYToque', fakeAsync(() => {
      const { f, servicio } = armar(conAna, 'panel');
      tecla();
      document.dispatchEvent(new MouseEvent('click'));
      document.dispatchEvent(new Event('touchstart'));
      expect(servicio.marcarServido).not.toHaveBeenCalled();
      f.destroy();
    }));

    it('panel_botonMarcaSoloUnaVez', fakeAsync(() => {
      const { f, servicio } = armar(conAna, 'panel');
      const boton: HTMLButtonElement = f.nativeElement.querySelector('button');
      boton.click();
      boton.click();
      expect(servicio.marcarServido).toHaveBeenCalledOnceWith('c1');
      tick(1500);
      f.destroy();
    }));
  });
});
