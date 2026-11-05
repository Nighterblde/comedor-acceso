import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { LoginEmpleadoComponent } from './login-empleado.component';
import { PaseComponent } from './pase.component';

const routes: Routes = [
  { path: 'pase', component: PaseComponent },
  { path: 'registro', component: PaseComponent, data: { registro: true } },
];

@NgModule({
  declarations: [PaseComponent, LoginEmpleadoComponent],
  imports: [CommonModule, ReactiveFormsModule, RouterModule.forChild(routes)],
})
export class PaseModule {}
