import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { LoginComponent } from './login.component';

describe('inicio de sesión', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [ReactiveFormsModule],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  function entrar(): { componente: LoginComponent; http: HttpTestingController } {
    const fixture = TestBed.createComponent(LoginComponent);
    const componente = fixture.componentInstance;
    componente.formulario.setValue({ usuario: 'chef', contrasena: 'secreta' });
    componente.entrar();
    return { componente, http: TestBed.inject(HttpTestingController) };
  }

  it('login_sinReturnUrl_navegaAInicio', () => {
    const navegar = spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
    const { http } = entrar();

    http
      .expectOne(`${environment.apiUrl}/api/auth/login`)
      .flush({ token: 't', rol: 'Chef', expiraEn: '2036-11-05T00:00:00Z', inicio: '/punto/P1' });

    expect(localStorage.getItem('comedor.inicio')).toBe('/punto/P1');
    expect(navegar).toHaveBeenCalledWith('/punto/P1');
  });

  it('Login_ChefSinPuntoActivo_409', () => {
    const { componente, http } = entrar();

    http
      .expectOne(`${environment.apiUrl}/api/auth/login`)
      .flush(
        { error: 'No hay un punto de entrada activo; pide al administrador que lo cree.' },
        { status: 409, statusText: 'Conflict' },
      );

    expect(componente.error).toBe('No hay un punto de entrada activo; pide al administrador que lo cree.');
    expect(localStorage.getItem('comedor.token.sistema')).toBeNull();
  });
});
