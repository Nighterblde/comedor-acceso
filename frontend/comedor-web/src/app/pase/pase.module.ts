import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Las rutas /pase y /registro viven aqui; la Tarea 12 les asigna componentes.
const routes: Routes = [
  { path: 'pase', children: [] },
  { path: 'registro', children: [] },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
})
export class PaseModule {}
