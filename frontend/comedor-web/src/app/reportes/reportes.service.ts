import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FilaConteo, FilaCotejo, FilaDetalle, FiltrosDetalle } from '../modelos/reporte.model';

/** Fechas en formato aaaa-MM-dd; el rango es inclusivo sobre la fecha de servicio. */
@Injectable({ providedIn: 'root' })
export class ReportesService {
  private readonly base = `${environment.apiUrl}/api/reportes`;

  constructor(private readonly http: HttpClient) {}

  conteo(desde: string, hasta: string): Observable<FilaConteo[]> {
    return this.http.get<FilaConteo[]>(`${this.base}/conteo`, { params: rango(desde, hasta) });
  }

  cotejo(desde: string, hasta: string): Observable<FilaCotejo[]> {
    return this.http.get<FilaCotejo[]>(`${this.base}/cotejo`, { params: rango(desde, hasta) });
  }

  detalle(desde: string, hasta: string, filtros: FiltrosDetalle = {}): Observable<FilaDetalle[]> {
    return this.http.get<FilaDetalle[]>(`${this.base}/detalle`, { params: conFiltros(rango(desde, hasta), filtros) });
  }

  /** Descarga `GET /api/reportes/<ruta>` como archivo con el nombre indicado. */
  descargar(ruta: string, nombreArchivo: string): Observable<void> {
    return this.http.get(`${this.base}/${ruta}`, { responseType: 'blob' }).pipe(
      map((blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = nombreArchivo;
        enlace.click();
        URL.revokeObjectURL(url);
      }),
    );
  }
}

function rango(desde: string, hasta: string): HttpParams {
  return new HttpParams().set('desde', desde).set('hasta', hasta);
}

/** Agrega solo los filtros con valor. */
export function conFiltros(params: HttpParams, filtros: FiltrosDetalle): HttpParams {
  for (const [clave, valor] of Object.entries(filtros)) {
    const texto = typeof valor === 'string' ? valor.trim() : '';
    if (texto) {
      params = params.set(clave, texto);
    }
  }
  return params;
}
