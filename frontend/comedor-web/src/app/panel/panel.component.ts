import { Component } from '@angular/core';
import { AuthService } from '../core/auth.service';

/** Marco del panel único: barra superior, aviso de sincronización y navegación lateral (D-37). */
@Component({
  selector: 'app-panel',
  standalone: false,
  templateUrl: './panel.component.html',
  styleUrl: './panel.component.scss',
})
export class PanelComponent {
  readonly grupos = [
    { titulo: 'Comedor', enlaces: [{ ruta: 'fila', etiqueta: 'Fila' }] },
    {
      titulo: 'Reportes',
      enlaces: [
        { ruta: 'cotejo', etiqueta: 'Cotejo de factura' },
        { ruta: 'conteo', etiqueta: 'Conteo' },
        { ruta: 'detalle', etiqueta: 'Detalle' },
      ],
    },
    {
      titulo: 'Administración',
      enlaces: [
        { ruta: 'reglas', etiqueta: 'Reglas' },
        { ruta: 'puntos', etiqueta: 'Puntos' },
        { ruta: 'usuarios', etiqueta: 'Usuarios' },
        { ruta: 'personal', etiqueta: 'Personal' },
        { ruta: 'sincronizacion', etiqueta: 'Sincronización' },
        { ruta: 'contingencia', etiqueta: 'Contingencia' },
      ],
    },
  ];

  constructor(readonly auth: AuthService) {}
}
