import { Component, OnInit, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { PuntoEntrada } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

function puntoVacio(): PuntoEntrada {
  return { id: '', nombre: '', activo: true };
}

/** Alta, edición y baja de puntos, y la vista imprimible del QR permanente (D-21). */
@Component({
  selector: 'app-puntos',
  standalone: false,
  templateUrl: './puntos.component.html',
  styleUrl: './puntos.component.scss',
})
export class PuntosComponent implements OnInit {
  readonly puntos = signal<PuntoEntrada[]>([]);
  readonly error = signal<string | null>(null);
  /** Desactiva Guardar y Eliminar mientras la API responde, para evitar dobles envíos (diseño §5). */
  readonly guardando = signal(false);
  readonly qr = signal<{ punto: PuntoEntrada; url: string } | null>(null);
  punto: PuntoEntrada = puntoVacio();
  /** Al editar, la clave no cambia: cambiarla crearía otro punto en lugar de renombrar (D-23). */
  editando = false;

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.cargar();
  }

  editar(p: PuntoEntrada): void {
    this.punto = { ...p };
    this.editando = true;
    this.error.set(null);
  }

  nuevo(): void {
    this.punto = puntoVacio();
    this.editando = false;
    this.error.set(null);
  }

  guardar(): void {
    if (this.guardando()) {
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    this.admin.guardarPunto(this.punto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.punto = puntoVacio();
        this.editando = false;
        this.cargar();
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.guardando.set(false);
      },
    });
  }

  verQr(p: PuntoEntrada): void {
    this.error.set(null);
    this.admin.qrPunto(p.id).subscribe({
      next: (r) => this.qr.set({ punto: p, url: r.url }),
      error: (e) => this.error.set(textoDeError(e)),
    });
  }

  imprimir(): void {
    window.print();
  }

  cerrarQr(): void {
    this.qr.set(null);
  }

  private cargar(): void {
    this.admin.puntos().subscribe({ next: (p) => this.puntos.set(p), error: (e) => this.error.set(textoDeError(e)) });
  }
}
