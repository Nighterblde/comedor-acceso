import { Component, signal } from '@angular/core';
import { AdminService } from '../admin/admin.service';
import { textoDeError } from '../compartido/errores';
import { ResultadoSincronizacion } from '../modelos/administracion.model';

/** Importación del Excel de personal (RF-36, D-42): plantilla descargable, archivo y resultado. */
@Component({
  selector: 'app-personal',
  standalone: false,
  templateUrl: './personal.component.html',
  styleUrl: '../admin/admin-pestana.scss',
})
export class PersonalComponent {
  readonly archivo = signal<File | null>(null);
  readonly resultado = signal<ResultadoSincronizacion | null>(null);
  readonly error = signal<string | null>(null);
  /** Desactiva Importar mientras la API responde, para evitar dobles envíos. */
  readonly importando = signal(false);

  constructor(private readonly admin: AdminService) {}

  elegir(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    this.archivo.set(entrada.files?.item(0) ?? null);
    this.resultado.set(null);
    this.error.set(null);
  }

  descargarPlantilla(): void {
    this.error.set(null);
    this.admin.plantillaPersonal().subscribe({ error: (e) => this.error.set(textoDeError(e)) });
  }

  importar(): void {
    const archivo = this.archivo();
    if (!archivo || this.importando()) {
      return;
    }
    this.error.set(null);
    this.resultado.set(null);
    this.importando.set(true);
    this.admin.importarPersonal(archivo).subscribe({
      next: (r) => {
        this.resultado.set(r);
        this.importando.set(false);
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.importando.set(false);
      },
    });
  }
}
