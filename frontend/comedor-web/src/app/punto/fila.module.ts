import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FilaComponent } from './fila.component';

/** La fila de servicio, compartida por la pantalla del chef y el panel del administrador. */
@NgModule({
  declarations: [FilaComponent],
  imports: [CommonModule],
  exports: [FilaComponent],
})
export class FilaModule {}
