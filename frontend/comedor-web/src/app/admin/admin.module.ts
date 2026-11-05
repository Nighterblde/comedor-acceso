import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { QRCodeComponent } from 'angularx-qrcode';
import { CompartidoModule } from '../compartido/compartido.module';
import { AdminComponent } from './admin.component';
import { ContingenciaComponent } from './contingencia.component';
import { PuntosComponent } from './puntos.component';
import { ReglasComponent } from './reglas.component';
import { SincronizacionComponent } from './sincronizacion.component';
import { UsuariosComponent } from './usuarios.component';

const routes: Routes = [
  {
    path: '',
    component: AdminComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'reglas' },
      { path: 'reglas', component: ReglasComponent },
      { path: 'puntos', component: PuntosComponent },
      { path: 'usuarios', component: UsuariosComponent },
      { path: 'sincronizacion', component: SincronizacionComponent },
      { path: 'contingencia', component: ContingenciaComponent },
    ],
  },
];

@NgModule({
  declarations: [
    AdminComponent,
    ReglasComponent,
    PuntosComponent,
    UsuariosComponent,
    SincronizacionComponent,
    ContingenciaComponent,
  ],
  imports: [CommonModule, FormsModule, CompartidoModule, QRCodeComponent, RouterModule.forChild(routes)],
})
export class AdminModule {}
