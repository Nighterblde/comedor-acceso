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
      { path: 'fila', title: 'Panel · Fila', component: FilaPanelComponent },
      { path: 'cotejo', title: 'Panel · Cotejo de factura', component: ReportesComponent, data: { tipo: 'cotejo' } },
      { path: 'conteo', title: 'Panel · Conteo', component: ReportesComponent, data: { tipo: 'conteo' } },
      { path: 'detalle', title: 'Panel · Detalle', component: ReportesComponent, data: { tipo: 'detalle' } },
      { path: 'reglas', title: 'Panel · Reglas', component: ReglasComponent },
      { path: 'puntos', title: 'Panel · Puntos', component: PuntosComponent },
      { path: 'usuarios', title: 'Panel · Usuarios', component: UsuariosComponent },
      { path: 'personal', title: 'Panel · Personal', component: PersonalComponent },
      { path: 'sincronizacion', title: 'Panel · Sincronización', component: SincronizacionComponent },
      { path: 'contingencia', title: 'Panel · Contingencia', component: ContingenciaComponent },
    ],
  },
];

@NgModule({
  declarations: [PanelComponent, FilaPanelComponent, PersonalComponent],
  imports: [CommonModule, CompartidoModule, AdminModule, ReportesModule, FilaModule, RouterModule.forChild(rutasPanel)],
})
export class PanelModule {}
