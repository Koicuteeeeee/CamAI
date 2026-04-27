import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { LoginComponent } from './features/auth/login.component';
import { AuthGuard } from './core/auth/auth.guard';
import { FaceRegistrationComponent } from './features/face-registration/face-registration.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { 
      path: '', 
      component: DashboardComponent, 
      canActivate: [AuthGuard] 
    },
    {
      path: 'register-face',
      component: FaceRegistrationComponent,
      canActivate: [AuthGuard]
    },
    { path: '**', redirectTo: '' }
];
