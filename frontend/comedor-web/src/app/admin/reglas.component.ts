import { Component, OnInit, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { ReglaDerecho } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

function reglaVacia(): ReglaDerecho {
  return { turno: '', tiempoComida: '', inicio: '', fin: '', perteneceAJornadaAnterior: false };
}

@Component({
  selector: 'app-reglas',
  standalone: false,
  templateUrl: './reglas.component.html',
  styleUrl: './admin-pestana.scss',
})
export class ReglasComponent implements OnInit {
  readonly reglas = signal<ReglaDerecho[]>([]);
  readonly error = signal<string | null>(null);
  /** Desactiva Guardar y Eliminar mientras la API responde, para evitar dobles envíos (diseño §5). */
  readonly guardando = signal(false);
  regla: ReglaDerecho = reglaVacia();

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.cargar();
  }

  editar(r: ReglaDerecho): void {
    this.regla = { ...r };
    this.error.set(null);
  }

  nueva(): void {
    this.regla = reglaVacia();
    this.error.set(null);
  }

  guardar(): void {
    if (this.guardando()) {
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    this.admin.guardarRegla(this.regla).subscribe({
      next: () => {
        this.guardando.set(false);
        this.regla = reglaVacia();
        this.cargar();
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.guardando.set(false);
      },
    });
  }

  eliminar(r: ReglaDerecho): void {
    if (this.guardando() || !confirm(`¿Eliminar la regla ${r.turno} · ${r.tiempoComida}?`)) {
      return;
    }
    this.guardando.set(true);
    this.admin.eliminarRegla(r.turno, r.tiempoComida).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cargar();
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.guardando.set(false);
      },
    });
  }

  private cargar(): void {
    this.admin.reglas().subscribe({ next: (r) => this.reglas.set(r), error: (e) => this.error.set(textoDeError(e)) });
  }
}
