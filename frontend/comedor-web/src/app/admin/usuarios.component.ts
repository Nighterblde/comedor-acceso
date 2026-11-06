import { Component, OnInit, signal } from '@angular/core';
import { textoDeError } from '../compartido/errores';
import { UsuarioEdicion, UsuarioSistema } from '../modelos/administracion.model';
import { AdminService } from './admin.service';

function usuarioVacio(): UsuarioEdicion {
  return { usuario: '', rol: 'Chef', activo: true, contrasena: '' };
}

/** Alta, edición y baja de usuarios del sistema; la contraseña nunca se muestra. */
@Component({
  selector: 'app-usuarios',
  standalone: false,
  templateUrl: './usuarios.component.html',
  styleUrl: './admin-pestana.scss',
})
export class UsuariosComponent implements OnInit {
  readonly roles = ['Chef', 'Administrador'];
  readonly usuarios = signal<UsuarioSistema[]>([]);
  readonly error = signal<string | null>(null);
  /** Desactiva Guardar y Eliminar mientras la API responde, para evitar dobles envíos (diseño §5). */
  readonly guardando = signal(false);
  usuario: UsuarioEdicion = usuarioVacio();
  /** Al editar, el usuario no cambia: cambiarlo crearía otra cuenta y dejaría activa la anterior (D-23). */
  editando = false;

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.cargar();
  }

  editar(u: UsuarioSistema): void {
    this.usuario = { ...u, contrasena: '' };
    this.editando = true;
    this.error.set(null);
  }

  nuevo(): void {
    this.usuario = usuarioVacio();
    this.editando = false;
    this.error.set(null);
  }

  guardar(): void {
    if (this.guardando()) {
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    const { contrasena, ...datos } = this.usuario;
    const cuerpo: UsuarioEdicion = contrasena ? { ...datos, contrasena } : datos;
    this.admin.guardarUsuario(cuerpo).subscribe({
      next: () => {
        this.guardando.set(false);
        this.usuario = usuarioVacio();
        this.editando = false;
        this.cargar();
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.guardando.set(false);
      },
    });
  }

  private cargar(): void {
    this.admin.usuarios().subscribe({ next: (u) => this.usuarios.set(u), error: (e) => this.error.set(textoDeError(e)) });
  }
}
