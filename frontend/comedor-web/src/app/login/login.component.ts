import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  enviando = false;
  error: string | null = null;

  private readonly fb = inject(FormBuilder);

  readonly formulario = this.fb.nonNullable.group({
    usuario: ['', Validators.required],
    contrasena: ['', Validators.required],
  });

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly ruta: ActivatedRoute,
  ) {}

  entrar(): void {
    if (this.enviando) {
      return;
    }
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error = 'Escribe tu usuario y tu contraseña';
      return;
    }

    this.enviando = true;
    this.error = null;
    const { usuario, contrasena } = this.formulario.getRawValue();
    this.auth.login(usuario, contrasena).subscribe({
      next: () => {
        this.enviando = false;
        const destino = this.ruta.snapshot.queryParamMap.get('returnUrl');
        if (destino && destino.startsWith('/') && !destino.startsWith('//')) {
          void this.router.navigateByUrl(destino);
        }
      },
      error: (e) => {
        this.enviando = false;
        this.error =
          e?.status === 401
            ? 'Usuario o contraseña incorrectos'
            : 'No fue posible iniciar sesión. Intenta de nuevo.';
      },
    });
  }
}
