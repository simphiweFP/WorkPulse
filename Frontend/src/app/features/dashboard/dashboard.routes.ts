import { Routes } from '@angular/router';

export const dashboardRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./admin-dashboard.component').then((component) => component.AdminDashboardComponent)
  }
];
