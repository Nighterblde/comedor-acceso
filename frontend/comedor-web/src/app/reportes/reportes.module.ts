import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportesComponent } from './reportes.component';

/** Pantalla de reportes; las rutas cotejo, conteo y detalle las define el panel (PanelModule). */
@NgModule({
  declarations: [ReportesComponent],
  imports: [CommonModule, FormsModule],
  exports: [ReportesComponent],
})
export class ReportesModule {}
