import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { inicioGuard } from './core/inicio.guard';
import { rolGuard } from './core/rol.guard';

export const rutas: Routes = [
  { path: 'login', loadChildren: () => import('./login/login.module').then((m) => m.LoginModule) },
  // Ruta de los accesos directos: lleva al inicio del rol (o al login si no hay sesión).
  { path: 'inicio', canActivate: [inicioGuard], children: [] },
  {
    path: 'punto/:id',
    canActivate: [rolGuard(['Chef', 'Administrador'])],
    loadChildren: () => import('./punto/punto.module').then((m) => m.PuntoModule),
  },
  {
    path: 'panel',
    canActivate: [rolGuard(['Administrador'])],
    loadChildren: () => import('./panel/panel.module').then((m) => m.PanelModule),
  },
  // Rutas anteriores: el panel único las reemplaza (D-37).
  { path: 'reportes', redirectTo: 'panel' },
  { path: 'admin', redirectTo: 'panel' },
  // PaseModule define /pase y /registro sin guard; va al final para no tapar las demas rutas.
  { path: '', loadChildren: () => import('./pase/pase.module').then((m) => m.PaseModule) },
];

@NgModule({
  imports: [RouterModule.forRoot(rutas)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
