import { Component, OnInit, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { UsuarioEdicion, UsuarioSistema } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

function usuarioVacio(): UsuarioEdicion {
  return { usuario: '', rol: 'Encargado', activo: true, contrasena: '' };
}

/** Alta, edición y baja de usuarios del sistema; la contraseña nunca se muestra. */
@Component({
  selector: 'app-usuarios',
  standalone: false,
  templateUrl: './usuarios.component.html',
  styleUrl: './admin-pestana.scss',
})
export class UsuariosComponent implements OnInit {
  readonly roles = ['Punto', 'Encargado', 'Supervisor', 'Administrador'];
  readonly usuarios = signal<UsuarioSistema[]>([]);
  readonly error = signal<string | null>(null);
  usuario: UsuarioEdicion = usuarioVacio();

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.cargar();
  }

  editar(u: UsuarioSistema): void {
    this.usuario = { ...u, contrasena: '' };
    this.error.set(null);
  }

  nuevo(): void {
    this.usuario = usuarioVacio();
    this.error.set(null);
  }

  guardar(): void {
    this.error.set(null);
    const { contrasena, ...datos } = this.usuario;
    const cuerpo: UsuarioEdicion = contrasena ? { ...datos, contrasena } : datos;
    this.admin.guardarUsuario(cuerpo).subscribe({
      next: () => {
        this.usuario = usuarioVacio();
        this.cargar();
      },
      error: (e) => this.error.set(textoDeError(e)),
    });
  }

  private cargar(): void {
    this.admin.usuarios().subscribe({ next: (u) => this.usuarios.set(u), error: (e) => this.error.set(textoDeError(e)) });
  }
}
