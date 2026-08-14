import { Routes } from '@angular/router';

export const projectsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./projects.component').then((component) => component.ProjectsComponent)
  }
];