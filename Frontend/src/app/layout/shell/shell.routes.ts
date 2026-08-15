import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';

export const shellRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./shell.component').then((component) => component.ShellComponent),
    children: [
      {
        path: 'today',
        loadComponent: () => import('../../features/dashboard/dashboard.component').then((component) => component.DashboardComponent)
      },
      {
        path: 'dashboard',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () => import('../../features/dashboard/admin-dashboard.component').then((component) => component.AdminDashboardComponent)
      },
      {
        path: 'backlog',
        loadComponent: () => import('../../features/tasks/my-tasks.component').then((component) => component.MyTasksComponent)
      },
      {
        path: 'sprints',
        loadChildren: () => import('../../features/sprints/sprints.routes').then((routes) => routes.sprintsRoutes)
      },
      {
        path: 'my-tasks',
        loadComponent: () => import('../../features/tasks/my-tasks.component').then((component) => component.MyTasksComponent)
      },
      {
        path: 'clients',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadChildren: () => import('../../features/clients/clients.routes').then((routes) => routes.clientsRoutes)
      },
      {
        path: 'clients/:id',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () => import('../../features/clients/client-detail.component').then((component) => component.ClientDetailComponent)
      },
      {
        path: 'projects',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadChildren: () => import('../../features/projects/projects.routes').then((routes) => routes.projectsRoutes)
      },
      {
        path: 'projects/:id',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () => import('../../features/projects/project-detail.component').then((component) => component.ProjectDetailComponent)
      },
      {
        path: 'tasks',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadChildren: () => import('../../features/tasks/tasks.routes').then((routes) => routes.tasksRoutes)
      },
      {
        path: 'tasks/:id',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () => import('../../features/tasks/task-detail.component').then((component) => component.TaskDetailComponent)
      },
      {
        path: 'team',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadChildren: () => import('../../features/users/users.routes').then((routes) => routes.usersRoutes)
      },
      {
        path: 'profile',
        loadComponent: () => import('../../features/profile/profile.component').then((component) => component.ProfileComponent)
      },
      {
        path: 'access-denied',
        loadComponent: () => import('../access-denied/access-denied.component').then((component) => component.AccessDeniedComponent)
      },
      { path: '', pathMatch: 'full', redirectTo: 'today' }
    ]
  }
];
