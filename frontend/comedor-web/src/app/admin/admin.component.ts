import { Component } from '@angular/core';
import { AuthService } from '../core/auth.service';

/** Marco de Administración: barra, aviso de sincronización y navegación por pestañas. */
@Component({
  selector: 'app-admin',
  standalone: false,
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent {
  readonly pestanas = [
    { ruta: 'reglas', etiqueta: 'Reglas' },
    { ruta: 'puntos', etiqueta: 'Puntos' },
    { ruta: 'usuarios', etiqueta: 'Usuarios' },
    { ruta: 'sincronizacion', etiqueta: 'Sincronización' },
    { ruta: 'contingencia', etiqueta: 'Contingencia' },
  ];

  constructor(readonly auth: AuthService) {}
}
