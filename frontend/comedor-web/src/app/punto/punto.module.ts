import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FilaComponent } from './fila.component';
import { PuntoComponent } from './punto.component';

const routes: Routes = [{ path: '', component: PuntoComponent }];

@NgModule({
  declarations: [PuntoComponent, FilaComponent],
  imports: [CommonModule, RouterModule.forChild(routes)],
  exports: [FilaComponent],
})
export class PuntoModule {}
