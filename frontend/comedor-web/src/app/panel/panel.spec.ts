import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { AdminService } from '../admin/admin.service';
import { rutas } from '../app-routing-module';
import { SincronizacionService } from '../compartido/sincronizacion.service';
import { FilaServicio, SolicitudPunto } from '../modelos/consumo.model';
import { PuntoService } from '../punto/punto.service';
import { AHORA } from './horario';

const REGLA_COMIDA = {
  turno: 'Matutino',
  tiempoComida: 'Comida',
  inicio: '12:00',
  fin: '15:00',
  perteneceAJornadaAnterior: false,
};

const ana: SolicitudPunto = {
  consumoId: 'c1',
  fechaHora: '2026-11-06T12:00:00',
  matricula: 'A1',
  nombre: 'Ana Pérez',
  fotoUrl: null,
  turno: 'Matutino',
  tiempoComida: 'Comida',
  fechaServicio: '2026-11-06',
  autorizado: true,
  motivo: null,
  servido: false,
};
const conAna: FilaServicio = { actual: ana, siguientes: [], total: 1 };

interface Opciones {
  rol?: string | null;
  hora?: number;
  puntos?: { id: string; nombre: string; activo: boolean }[];
}

function configurar({ rol = 'Administrador', hora = 9, puntos }: Opciones = {}) {
  localStorage.clear();
  if (rol) {
    localStorage.setItem('comedor.token.sistema', 'tok');
    localStorage.setItem('comedor.rol', rol);
    localStorage.setItem('comedor.usuario', 'super');
    localStorage.setItem('comedor.expira', new Date(2026, 10, 6, 15, 42).toISOString());
  }
  const admin = jasmine.createSpyObj<AdminService>('AdminService', ['reglas', 'puntos']);
  admin.reglas.and.returnValue(of([REGLA_COMIDA]));
  admin.puntos.and.returnValue(of(puntos ?? [{ id: 'P1', nombre: 'Entrada', activo: true }]));
  const punto = jasmine.createSpyObj<PuntoService>('PuntoService', ['fila', 'marcarServido']);
  punto.fila.and.returnValue(of(conAna));
  punto.marcarServido.and.returnValue(of({ yaServido: false }));
  TestBed.configureTestingModule({
    providers: [
      provideRouter(rutas),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AdminService, useValue: admin },
      { provide: PuntoService, useValue: punto },
      { provide: SincronizacionService, useValue: { estado: signal(null), refrescar: () => undefined } },
      { provide: AHORA, useValue: () => new Date(2026, 10, 6, hora, 0) },
    ],
  });
  return { admin, punto };
}

async function abrir(url: string) {
  const arnes = await RouterTestingHarness.create();
  await arnes.navigateByUrl(url);
  await new Promise((r) => setTimeout(r, 20));
  arnes.detectChanges();
  return arnes;
}

describe('panel del administrador', () => {
  afterEach(() => localStorage.clear());

  it('panel_muestraLosTresGrupos', async () => {
    configurar();
    const arnes = await abrir('/panel/cotejo');
    const raiz: HTMLElement = arnes.routeNativeElement as HTMLElement;
    const titulos = Array.from(raiz.querySelectorAll('nav h2')).map((h) => h.textContent?.trim());
    expect(titulos).toEqual(['Comedor', 'Reportes', 'Administración']);
    const enlaces = Array.from(raiz.querySelectorAll('nav a')).map((a) => a.getAttribute('href'));
    expect(enlaces).toEqual([
      '/panel/fila',
      '/panel/cotejo',
      '/panel/conteo',
      '/panel/detalle',
      '/panel/reglas',
      '/panel/puntos',
      '/panel/usuarios',
      '/panel/personal',
      '/panel/sincronizacion',
      '/panel/contingencia',
    ]);
    expect(raiz.querySelectorAll('nav ul li a').length).toBe(10);
  });

  it('panel_enHorario_abreFila', async () => {
    configurar({ hora: 13 });
    await abrir('/panel');
    expect(TestBed.inject(Router).url).toBe('/panel/fila');
  });

  it('panel_fueraDeHorario_abreCotejo', async () => {
    configurar({ hora: 9 });
    await abrir('/panel');
    expect(TestBed.inject(Router).url).toBe('/panel/cotejo');
  });

  it('filaEnPanel_teclaNoMarca_botonSiMarca', async () => {
    const { punto } = configurar({ hora: 13 });
    const arnes = await abrir('/panel/fila');
    const raiz: HTMLElement = arnes.routeNativeElement as HTMLElement;

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'a' }));
    document.dispatchEvent(new Event('touchstart'));
    expect(punto.marcarServido).not.toHaveBeenCalled();

    const boton = Array.from(raiz.querySelectorAll('button')).find((b) =>
      (b.textContent ?? '').includes('Marcar servido'),
    ) as HTMLButtonElement;
    expect(boton).toBeDefined();
    boton.click();
    expect(punto.marcarServido).toHaveBeenCalledOnceWith('c1');
  });

  it('abrirPantallaDelChef_enlaceAPunto', async () => {
    configurar({
      hora: 13,
      puntos: [
        { id: 'P0', nombre: 'Cerrado', activo: false },
        { id: 'P3', nombre: 'Tres', activo: true },
        { id: 'P1', nombre: 'Uno', activo: true },
      ],
    });
    const arnes = await abrir('/panel/fila');
    const raiz: HTMLElement = arnes.routeNativeElement as HTMLElement;
    const enlace = Array.from(raiz.querySelectorAll('a')).find((a) =>
      (a.textContent ?? '').includes('Abrir pantalla del chef'),
    ) as HTMLAnchorElement;
    expect(enlace.getAttribute('href')).toBe('/punto/P1');
    expect(enlace.getAttribute('target')).toBe('_blank');
  });

  it('rutaReportes_redirigeAPanel', async () => {
    configurar();
    await abrir('/reportes');
    expect(TestBed.inject(Router).url).toMatch(/^\/panel/);
  });

  it('rutaAdmin_redirigeAPanel', async () => {
    configurar();
    await abrir('/admin');
    expect(TestBed.inject(Router).url).toMatch(/^\/panel/);
  });

  it('panel_conRolChef_vaALogin', async () => {
    configurar({ rol: 'Chef' });
    await abrir('/panel');
    const url = TestBed.inject(Router).url;
    expect(url).toMatch(/^\/login/);
    expect(url).toContain('returnUrl=%2Fpanel');
  });

  it('navegacion_marcaRutaActiva', async () => {
    configurar();
    const arnes = await abrir('/panel/conteo');
    const raiz: HTMLElement = arnes.routeNativeElement as HTMLElement;
    const activos = Array.from(raiz.querySelectorAll('nav a[aria-current="page"]'));
    expect(activos.length).toBe(1);
    expect(activos[0].textContent).toContain('Conteo');
  });

  it('panel_muestraHoraDeFinDeSesion', async () => {
    configurar();
    const arnes = await abrir('/panel/cotejo');
    const barra = (arnes.routeNativeElement as HTMLElement).querySelector('header') as HTMLElement;
    expect(barra.textContent).toContain('Sesión hasta las 15:42');
    expect(barra.textContent).toContain('super');
    expect(barra.textContent).toContain('Administrador');
  });
});
