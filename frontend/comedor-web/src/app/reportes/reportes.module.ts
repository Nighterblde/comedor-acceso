import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { CompartidoModule } from '../compartido/compartido.module';
import { ReportesComponent } from './reportes.component';

const routes: Routes = [{ path: '', component: ReportesComponent }];

@NgModule({
  declarations: [ReportesComponent],
  imports: [CommonModule, FormsModule, CompartidoModule, RouterModule.forChild(routes)],
})
export class ReportesModule {}
