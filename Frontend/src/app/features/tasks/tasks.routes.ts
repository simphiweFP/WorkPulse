import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';

export const tasksRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./tasks.component').then((component) => component.TasksComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./task-detail.component').then((component) => component.TaskDetailComponent)
  },
  {
    path: 'my-tasks',
    canActivate: [roleGuard],
    data: { role: 'Developer' },
    loadComponent: () => import('./my-tasks.component').then((component) => component.MyTasksComponent)
  }
];