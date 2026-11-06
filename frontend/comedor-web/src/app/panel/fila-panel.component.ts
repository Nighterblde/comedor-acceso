import { Component, OnInit, signal } from '@angular/core';
import { AdminService } from '../admin/admin.service';

/** La fila de la TV dentro del panel, con el enlace para abrir la pantalla del chef (D-37). */
@Component({
  selector: 'app-fila-panel',
  standalone: false,
  templateUrl: './fila-panel.component.html',
  styleUrl: './fila-panel.component.scss',
})
export class FilaPanelComponent implements OnInit {
  /** Primer punto activo por Id (comparación ordinal), el mismo criterio del inicio del chef (D-44). */
  readonly puntoId = signal<string | null>(null);
  readonly cargado = signal(false);

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.admin.puntos().subscribe({
      next: (puntos) => {
        const activos = puntos.filter((p) => p.activo).map((p) => p.id);
        activos.sort((a, b) => (a < b ? -1 : a > b ? 1 : 0));
        this.puntoId.set(activos[0] ?? null);
        this.cargado.set(true);
      },
      error: () => this.cargado.set(true),
    });
  }
}
