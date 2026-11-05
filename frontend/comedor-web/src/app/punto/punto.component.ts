import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, timer } from 'rxjs';

@Component({
  selector: 'app-punto',
  standalone: false,
  templateUrl: './punto.component.html',
  styleUrl: './punto.component.scss',
})
export class PuntoComponent implements OnInit, OnDestroy {
  puntoId = '';
  readonly hora = signal(new Date());

  private reloj?: Subscription;

  constructor(private readonly ruta: ActivatedRoute) {}

  ngOnInit(): void {
    this.puntoId = this.ruta.snapshot.paramMap.get('id') ?? '';
    this.reloj = timer(0, 1000).subscribe(() => this.hora.set(new Date()));
  }

  ngOnDestroy(): void {
    this.reloj?.unsubscribe();
  }
}
