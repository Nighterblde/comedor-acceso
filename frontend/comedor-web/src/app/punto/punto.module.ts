import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { CapturaManualComponent } from './captura-manual.component';
import { PuntoComponent } from './punto.component';
import { SolicitudesComponent } from './solicitudes.component';

const routes: Routes = [{ path: '', component: PuntoComponent }];

@NgModule({
  declarations: [PuntoComponent, SolicitudesComponent, CapturaManualComponent],
  imports: [CommonModule, FormsModule, RouterModule.forChild(routes)],
})
export class PuntoModule {}
