import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ClientsComponent } from './features/clients/clients.component';
import { ProjectsComponent } from './features/projects/projects.component';
import { TasksComponent } from './features/tasks/tasks.component';
import { MyTasksComponent } from './features/tasks/my-tasks.component';
import { AuthenticatedLayoutComponent } from './layout/authenticated-layout/authenticated-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    component: AuthenticatedLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'clients', component: ClientsComponent },
      { path: 'projects', component: ProjectsComponent },
      { path: 'tasks', component: TasksComponent },
      { path: 'my-tasks', component: MyTasksComponent, canActivate: [roleGuard], data: { role: 'Developer' } },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
