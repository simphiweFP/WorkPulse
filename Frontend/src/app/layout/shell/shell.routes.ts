import { Routes } from '@angular/router';

export const shellRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./shell.component').then((component) => component.ShellComponent),
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('../../features/dashboard/dashboard.routes').then((routes) => routes.dashboardRoutes)
      },
      {
        path: 'clients',
        loadChildren: () => import('../../features/clients/clients.routes').then((routes) => routes.clientsRoutes)
      },
      {
        path: 'clients/:id',
        loadComponent: () => import('../../features/clients/client-detail.component').then((component) => component.ClientDetailComponent)
      },
      {
        path: 'projects',
        loadChildren: () => import('../../features/projects/projects.routes').then((routes) => routes.projectsRoutes)
      },
      {
        path: 'projects/:id',
        loadComponent: () => import('../../features/projects/project-detail.component').then((component) => component.ProjectDetailComponent)
      },
      {
        path: 'tasks',
        loadChildren: () => import('../../features/tasks/tasks.routes').then((routes) => routes.tasksRoutes)
      },
      {
        path: 'my-tasks',
        canActivate: [],
        loadComponent: () => import('../../features/tasks/my-tasks.component').then((component) => component.MyTasksComponent)
      },
      {
        path: 'users',
        loadChildren: () => import('../../features/users/users.routes').then((routes) => routes.usersRoutes)
      },
      {
        path: 'access-denied',
        loadComponent: () => import('../access-denied/access-denied.component').then((component) => component.AccessDeniedComponent)
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  }
];