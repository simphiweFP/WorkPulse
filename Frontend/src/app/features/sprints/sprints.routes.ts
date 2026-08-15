import { Routes } from '@angular/router';

export const sprintsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./sprints.component').then((component) => component.SprintsComponent)
  }
];
