import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary } from '../../core/models/client.models';
import { ProjectSummary, ProjectUpsertRequest } from '../../core/models/project.models';
import { ClientService } from '../../core/services/client.service';
import { ProjectService } from '../../core/services/project.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      <app-page-header eyebrow="Projects" title="Projects" subtitle="Track project health and the work attached to each client." />

      @if (loading()) {
        <app-loading-state message="Loading projects..." />
      } @else {
        <form class="panel form-grid" [formGroup]="form" (ngSubmit)="saveProject()">
          <label>Project<input formControlName="name" /></label>
          <label>
            Client
            <select formControlName="clientId">
              <option value="">Select client</option>
              @for (client of clients(); track client.id) {
                <option [value]="client.id">{{ client.name }}</option>
              }
            </select>
          </label>
          <label>
            Status
            <select formControlName="status">
              <option value="Active">Active</option>
              <option value="Completed">Completed</option>
              <option value="Archived">Archived</option>
            </select>
          </label>
          <label class="full-width">Description<textarea rows="4" formControlName="description"></textarea></label>
          <div class="actions full-width">
            <button type="submit" [disabled]="submitting()">{{ selectedProjectId() ? 'Update' : 'Create' }} Project</button>
            @if (selectedProjectId()) {
              <button type="button" class="secondary" (click)="resetForm()">Cancel</button>
            }
          </div>
        </form>

        @if (projects().length) {
          <div class="table-card">
            <table>
              <thead>
                <tr>
                  <th>Project</th>
                  <th>Client</th>
                  <th>Status</th>
                  <th>Open Tasks</th>
                  <th>Completed Tasks</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (project of projects(); track project.id) {
                  <tr>
                    <td><a [routerLink]="['/projects', project.id]">{{ project.name }}</a></td>
                    <td>{{ project.clientName }}</td>
                    <td>{{ project.status }}</td>
                    <td>{{ project.openTasks }}</td>
                    <td>{{ project.completedTasks }}</td>
                    <td class="row-actions">
                      <button type="button" class="secondary" (click)="selectProject(project)">Edit</button>
                      <button type="button" class="danger" (click)="removeProject(project.id)">Delete</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <app-empty-state title="No projects yet." message="Add the first project to begin tracking delivery." />
        }
      }
    </section>
  `
})
export class ProjectsComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly clientService = inject(ClientService);
  private readonly fb = inject(FormBuilder);

  readonly projects = signal<ProjectSummary[]>([]);
  readonly clients = signal<ClientSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly selectedProjectId = signal('');

  readonly form = this.fb.group({
    name: ['', Validators.required],
    clientId: ['', Validators.required],
    status: ['Active', Validators.required],
    description: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.projectService.getProjects().subscribe((projects) => this.projects.set(projects));
    this.clientService
      .getClients()
      .pipe(
        catchError(() => of([] as ClientSummary[])),
        finalize(() => this.loading.set(false))
      )
      .subscribe((clients) => this.clients.set(clients));
  }

  selectProject(project: ProjectSummary): void {
    this.selectedProjectId.set(project.id);
    this.form.patchValue({
      name: project.name,
      clientId: '',
      status: project.status,
      description: ''
    });
  }

  resetForm(): void {
    this.selectedProjectId.set('');
    this.form.reset({ status: 'Active' });
  }

  saveProject(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue() as ProjectUpsertRequest;
    const action$ = this.selectedProjectId()
      ? this.projectService.updateProject(this.selectedProjectId(), request)
      : this.projectService.createProject(request);

    this.submitting.set(true);
    action$.pipe(finalize(() => this.submitting.set(false))).subscribe(() => {
      this.resetForm();
      this.loadData();
    });
  }

  removeProject(id: string): void {
    this.projectService.deleteProject(id).subscribe(() => this.loadData());
  }
}