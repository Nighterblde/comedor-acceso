import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { inicioGuard } from './core/inicio.guard';
import { rolGuard } from './core/rol.guard';

const routes: Routes = [
  { path: 'login', loadChildren: () => import('./login/login.module').then((m) => m.LoginModule) },
  // Ruta de los accesos directos: lleva al inicio del rol (o al login si no hay sesión).
  { path: 'inicio', canActivate: [inicioGuard], children: [] },
  {
    path: 'punto/:id',
    canActivate: [rolGuard(['Chef', 'Administrador'])],
    loadChildren: () => import('./punto/punto.module').then((m) => m.PuntoModule),
  },
  {
    path: 'reportes',
    canActivate: [rolGuard(['Administrador'])],
    loadChildren: () => import('./reportes/reportes.module').then((m) => m.ReportesModule),
  },
  {
    path: 'admin',
    canActivate: [rolGuard(['Administrador'])],
    loadChildren: () => import('./admin/admin.module').then((m) => m.AdminModule),
  },
  // PaseModule define /pase y /registro sin guard; va al final para no tapar las demas rutas.
  { path: '', loadChildren: () => import('./pase/pase.module').then((m) => m.PaseModule) },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
