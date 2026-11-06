import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FilaModule } from './fila.module';
import { PuntoComponent } from './punto.component';

const routes: Routes = [{ path: '', component: PuntoComponent }];

@NgModule({
  declarations: [PuntoComponent],
  imports: [CommonModule, FilaModule, RouterModule.forChild(routes)],
})
export class PuntoModule {}
