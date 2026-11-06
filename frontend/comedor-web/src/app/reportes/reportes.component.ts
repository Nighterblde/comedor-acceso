import { Component, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { textoDeError } from '../compartido/errores';
import { Canal, FilaConteo, FilaCotejo, FilaDetalle, FiltrosDetalle, MotivoRechazo } from '../modelos/reporte.model';
import { conFiltros, ReportesService } from './reportes.service';
import { HttpParams } from '@angular/common/http';

export type TipoReporte = 'cotejo' | 'conteo' | 'detalle';

@Component({
  selector: 'app-reportes',
  standalone: false,
  templateUrl: './reportes.component.html',
  styleUrl: './reportes.component.scss',
})
export class ReportesComponent {
  readonly canales: Canal[] = ['Celular', 'Manual', 'Contingencia'];
  readonly motivos: { valor: MotivoRechazo; texto: string }[] = [
    { valor: 'NoRegistrado', texto: 'No registrado' },
    { valor: 'ContratoVencido', texto: 'Contrato vencido' },
    { valor: 'DiaDescanso', texto: 'Hoy es tu día de descanso' },
    { valor: 'FueraDeHorario', texto: 'Fuera de horario' },
    { valor: 'TurnoSinDerecho', texto: 'Tu turno no tiene derecho a este tiempo de comida' },
    { valor: 'YaConsumio', texto: 'Ya consumiste este tiempo de comida' },
  ];

  /** Tipo de reporte: lo fija la ruta (/panel/cotejo, /panel/conteo, /panel/detalle). */
  tipo: TipoReporte;
  desde = '';
  hasta = '';
  filtros: FiltrosDetalle = {};

  readonly cotejo = signal<FilaCotejo[] | null>(null);
  readonly conteo = signal<FilaConteo[] | null>(null);
  readonly detalle = signal<FilaDetalle[] | null>(null);
  readonly error = signal<string | null>(null);
  readonly cargando = signal(false);

  constructor(
    private readonly reportes: ReportesService,
    ruta: ActivatedRoute,
  ) {
    this.tipo = ruta.snapshot.data['tipo'] ?? 'cotejo';
  }

  get rangoCompleto(): boolean {
    return !!this.desde && !!this.hasta;
  }

  get puedeExportarPdf(): boolean {
    return this.tipo !== 'detalle';
  }

  /** Al cambiar el rango o los filtros se ocultan los resultados: lo visible debe coincidir con lo que se exporta. */
  limpiarResultados(): void {
    this.cotejo.set(null);
    this.conteo.set(null);
    this.detalle.set(null);
  }

  consultar(): void {
    if (!this.rangoCompleto) {
      return;
    }
    this.error.set(null);
    this.cargando.set(true);
    if (this.tipo === 'cotejo') {
      this.cargar(this.reportes.cotejo(this.desde, this.hasta), (f) => this.cotejo.set(f));
    } else if (this.tipo === 'conteo') {
      this.cargar(this.reportes.conteo(this.desde, this.hasta), (f) => this.conteo.set(f));
    } else {
      this.cargar(this.reportes.detalle(this.desde, this.hasta, this.filtros), (f) => this.detalle.set(f));
    }
  }

  exportar(formato: 'xlsx' | 'pdf'): void {
    if (!this.rangoCompleto) {
      return;
    }
    let params = new HttpParams().set('desde', this.desde).set('hasta', this.hasta);
    if (this.tipo === 'detalle') {
      params = conFiltros(params, this.filtros);
    }
    const ruta = `${this.tipo}.${formato}?${params.toString()}`;
    const nombre = `${this.tipo}-${this.desde}-${this.hasta}.${formato}`;
    this.error.set(null);
    this.reportes.descargar(ruta, nombre).subscribe({ error: (e) => this.error.set(textoDeError(e)) });
  }

  totalCotejo(filas: FilaCotejo[]): { servidas: number; sinServir: number } {
    return filas.reduce(
      (t, f) => ({ servidas: t.servidas + f.servidas, sinServir: t.sinServir + f.sinServir }),
      { servidas: 0, sinServir: 0 },
    );
  }

  totalConteo(filas: FilaConteo[]): number {
    return filas.reduce((t, f) => t + f.total, 0);
  }

  private cargar<T>(consulta: Observable<T>, asignar: (filas: T) => void): void {
    consulta.subscribe({
      next: (filas) => {
        asignar(filas);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(textoDeError(e));
        this.cargando.set(false);
      },
    });
  }
}
