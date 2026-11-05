import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { PaseService } from './pase.service';

@Component({
  selector: 'app-login-empleado',
  standalone: false,
  templateUrl: './login-empleado.component.html',
  styleUrl: './login-empleado.component.scss',
})
export class LoginEmpleadoComponent {
  @Input() punto: string | null = null;
  @Output() sesionIniciada = new EventEmitter<void>();

  enviando = false;
  error: string | null = null;

  private readonly fb = inject(FormBuilder);

  readonly formulario = this.fb.nonNullable.group({
    id: ['', Validators.required],
    contrasena: ['', Validators.required],
  });

  constructor(private readonly pase: PaseService) {}

  entrar(): void {
    if (this.enviando || this.formulario.invalid) {
      return;
    }
    this.enviando = true;
    this.error = null;
    const { id, contrasena } = this.formulario.getRawValue();
    this.pase.iniciarSesion(id, contrasena).subscribe({
      next: () => {
        this.enviando = false;
        this.sesionIniciada.emit();
      },
      error: (e: Error) => {
        this.enviando = false;
        this.error = e.message;
      },
    });
  }
}
