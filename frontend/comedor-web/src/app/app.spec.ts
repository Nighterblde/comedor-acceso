import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter, Router, RouterModule, TitleStrategy } from '@angular/router';
import { App } from './app';
import { rutasPanel } from './panel/panel.module';
import { TituloStrategy } from './core/titulo.strategy';
import indexHtml from '../index.html';

@Component({ selector: 'app-vacio', standalone: true, template: '' })
class VacioComponent {}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RouterModule.forRoot([])],
      declarations: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('router-outlet')).not.toBeNull();
  });
});

describe('index.html', () => {
  it('index_sinFaviconDeAngular_conIconoPropio', () => {
    expect(indexHtml).not.toContain('favicon.ico');
    expect(indexHtml).toContain('<link rel="icon" type="image/svg+xml" href="icono.svg">');
    expect(indexHtml).toContain('<meta name="theme-color" content="#10312B">');
    expect(indexHtml).toContain('<title>Comedor de personal</title>');
  });

  it('index_sinGoogleFonts', () => {
    expect(indexHtml).not.toContain('fonts.googleapis');
    expect(indexHtml).not.toContain('fonts.gstatic');
  });
});

describe('títulos', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: TitleStrategy, useClass: TituloStrategy },
        provideRouter([
          { path: 'pase', title: 'Pase', component: VacioComponent },
          {
            path: 'panel',
            component: VacioComponent,
            children: [{ path: 'cotejo', title: 'Panel · Cotejo de factura', component: VacioComponent }],
          },
          { path: 'sin-titulo', component: VacioComponent },
        ]),
      ],
    });
  });

  it('titulo_cambiaPorSeccion', async () => {
    const router = TestBed.inject(Router);
    const titulo = TestBed.inject(Title);

    await router.navigateByUrl('/pase');
    expect(titulo.getTitle()).toBe('Pase · Comedor de personal');

    await router.navigateByUrl('/panel/cotejo');
    expect(titulo.getTitle()).toBe('Panel · Cotejo de factura · Comedor de personal');

    await router.navigateByUrl('/sin-titulo');
    expect(titulo.getTitle()).toBe('Comedor de personal');
  });

  it('titulo_todaSeccionDelPanelTieneNombre', () => {
    const hijas = rutasPanel[0].children!.filter((r) => r.component);
    expect(hijas.length).toBe(10);
    for (const r of hijas) {
      expect(r.title).toMatch(/^Panel · .+/, r.path);
    }
  });
});
