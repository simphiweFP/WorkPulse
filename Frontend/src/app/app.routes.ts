import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/login/login.routes').then((routes) => routes.loginRoutes)
  },
  {
    path: 'register',
    loadChildren: () => import('./features/auth/register/register.routes').then((routes) => routes.registerRoutes)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () => import('./layout/shell/shell.routes').then((routes) => routes.shellRoutes)
  },
  { path: '**', redirectTo: 'dashboard' }
];
