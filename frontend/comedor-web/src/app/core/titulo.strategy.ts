import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

export const NOMBRE_SISTEMA = 'Comedor de personal';

/** Título de la pestaña: «{sección} · Comedor de personal»; sin sección, solo el nombre del sistema. */
@Injectable({ providedIn: 'root' })
export class TituloStrategy extends TitleStrategy {
  constructor(private readonly titulo: Title) {
    super();
  }

  override updateTitle(estado: RouterStateSnapshot): void {
    const seccion = this.buildTitle(estado);
    this.titulo.setTitle(seccion ? `${seccion} · ${NOMBRE_SISTEMA}` : NOMBRE_SISTEMA);
  }
}
