import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminModule } from '../admin/admin.module';
import { ContingenciaComponent } from '../admin/contingencia.component';
import { PuntosComponent } from '../admin/puntos.component';
import { ReglasComponent } from '../admin/reglas.component';
import { SincronizacionComponent } from '../admin/sincronizacion.component';
import { UsuariosComponent } from '../admin/usuarios.component';
import { CompartidoModule } from '../compartido/compartido.module';
import { FilaModule } from '../punto/fila.module';
import { ReportesComponent } from '../reportes/reportes.component';
import { ReportesModule } from '../reportes/reportes.module';
import { FilaPanelComponent } from './fila-panel.component';
import { PanelComponent } from './panel.component';
import { panelInicioGuard } from './panel-inicio.guard';
import { PersonalComponent } from './personal.component';

export const rutasPanel: Routes = [
  {
    path: '',
    component: PanelComponent,
    children: [
      { path: '', pathMatch: 'full', canActivate: [panelInicioGuard], children: [] },
      { path: 'fila', component: FilaPanelComponent },
      { path: 'cotejo', component: ReportesComponent, data: { tipo: 'cotejo' } },
      { path: 'conteo', component: ReportesComponent, data: { tipo: 'conteo' } },
      { path: 'detalle', component: ReportesComponent, data: { tipo: 'detalle' } },
      { path: 'reglas', component: ReglasComponent },
      { path: 'puntos', component: PuntosComponent },
      { path: 'usuarios', component: UsuariosComponent },
      { path: 'personal', component: PersonalComponent },
      { path: 'sincronizacion', component: SincronizacionComponent },
      { path: 'contingencia', component: ContingenciaComponent },
    ],
  },
];

@NgModule({
  declarations: [PanelComponent, FilaPanelComponent, PersonalComponent],
  imports: [CommonModule, CompartidoModule, AdminModule, ReportesModule, FilaModule, RouterModule.forChild(rutasPanel)],
})
export class PanelModule {}
