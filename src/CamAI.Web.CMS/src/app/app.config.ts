import { ApplicationConfig, provideBrowserGlobalErrorListeners, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { LucideAngularModule, Camera, Users, Car, RefreshCw, Settings, ChevronLeft, LogOut, User, Lock, Eye, EyeOff, ShieldCheck, Cpu, ArrowRight } from 'lucide-angular';
import { KeycloakService, KeycloakAngularModule } from 'keycloak-angular';

import { routes } from './app.routes';

function initializeKeycloak(keycloak: KeycloakService) {
  return () =>
    keycloak.init({
      config: {
        url: 'http://192.168.1.144:8081',
        realm: 'CamAI',
        clientId: 'camai-web'
      },
      initOptions: {
        checkLoginIframe: false,
        // Không dùng onLoad để tránh Keycloak tự động redirect khi khởi động App
      },
      bearerExcludedUrls: ['/assets', '/clients/public']
    }).catch(err => {
      console.warn('Keycloak init failed, continuing to internal login:', err);
      return true;
    });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    importProvidersFrom(
      KeycloakAngularModule,
      LucideAngularModule.pick({ 
        Camera, Users, Car, RefreshCw, Settings, ChevronLeft, LogOut, User,
        Lock, Eye, EyeOff, ShieldCheck, Cpu, ArrowRight
      })
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeKeycloak,
      multi: true,
      deps: [KeycloakService]
    }
  ]
};
