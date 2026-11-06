import { NgModule, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { TitleStrategy } from '@angular/router';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { CoreModule } from './core/core.module';
import { TituloStrategy } from './core/titulo.strategy';

@NgModule({
  declarations: [
    App
  ],
  imports: [
    BrowserModule,
    CoreModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: TitleStrategy, useClass: TituloStrategy },
    provideZoneChangeDetection({ eventCoalescing: true }),
  ],
  bootstrap: [App]
})
export class AppModule { }
