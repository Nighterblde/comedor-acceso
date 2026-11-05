import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { AvisoSincronizacionComponent } from './aviso-sincronizacion.component';

/** Piezas de vista que comparten Reportes y Administración. */
@NgModule({
  declarations: [AvisoSincronizacionComponent],
  imports: [CommonModule],
  exports: [AvisoSincronizacionComponent],
})
export class CompartidoModule {}
