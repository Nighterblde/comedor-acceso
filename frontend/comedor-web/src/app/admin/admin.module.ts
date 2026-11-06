import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QRCodeComponent } from 'angularx-qrcode';
import { CompartidoModule } from '../compartido/compartido.module';
import { ContingenciaComponent } from './contingencia.component';
import { PuntosComponent } from './puntos.component';
import { ReglasComponent } from './reglas.component';
import { SincronizacionComponent } from './sincronizacion.component';
import { UsuariosComponent } from './usuarios.component';

const PANTALLAS = [ReglasComponent, PuntosComponent, UsuariosComponent, SincronizacionComponent, ContingenciaComponent];

/** Pantallas de administración; las rutas las define el panel (PanelModule). */
@NgModule({
  declarations: PANTALLAS,
  imports: [CommonModule, FormsModule, CompartidoModule, QRCodeComponent],
  exports: PANTALLAS,
})
export class AdminModule {}
