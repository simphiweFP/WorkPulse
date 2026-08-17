import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary } from '../../core/models/client.models';
import { ProjectSummary, ProjectUpsertRequest } from '../../core/models/project.models';
import { ClientService } from '../../core/services/client.service';
import { ProjectService } from '../../core/services/project.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './projects.component.scss',
  templateUrl: './projects.component.html'
})
export class ProjectsComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly clientService = inject(ClientService);
  private readonly fb = inject(FormBuilder);
  private readonly alerts = inject(FeedbackAlertService);

  readonly projects = signal<ProjectSummary[]>([]);
  readonly clients = signal<ClientSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly selectedProjectId = signal('');
  readonly selectedProject = signal<ProjectSummary | null>(null);
  readonly error = signal('');
  readonly formError = signal('');
  readonly modalOpen = signal(false);
  // Search removed per request

  readonly form = this.fb.group({
    name: ['', Validators.required],
    clientId: ['', Validators.required],
    totalTasks: [0, Validators.required],
    startDate: ['', Validators.required],
    status: ['', Validators.required],
    description: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');
    this.projectService.getProjects().pipe(
      catchError(() => {
        this.error.set('We could not load projects right now. Please try again.');
        return of([] as ProjectSummary[]);
      })
    ).subscribe((projects) => this.projects.set(projects));

    this.clientService
      .getClients()
      .pipe(
        catchError(() => of([] as ClientSummary[])),
        finalize(() => this.loading.set(false))
      )
      .subscribe((clients) => this.clients.set(clients));
  }

  filteredProjects(): ProjectSummary[] {
    // Search removed — show all projects
    return this.projects();
  }

  openCreateProject(): void {
    this.selectedProjectId.set('');
    this.selectedProject.set(null);
    this.form.reset({ status: '', startDate: '', totalTasks: 0 });
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  selectProject(project: ProjectSummary): void {
    this.selectedProjectId.set(project.id);
    this.selectedProject.set(project);
    this.form.patchValue({
      clientId: project.clientId,
      name: project.name,
      totalTasks: project.totalTasks,
      startDate: this.toDateInputValue(project.startDate),
      status: project.status,
      description: ''
    });
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.selectedProjectId.set('');
    this.selectedProject.set(null);
    this.form.reset({ status: '', startDate: '', totalTasks: 0 });
    this.formError.set('');
    unlockBodyScroll();
  }

  saveProject(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required project fields.');
      return;
    }

    const request = this.form.getRawValue() as ProjectUpsertRequest;
    this.submitting.set(true);
    this.formError.set('');
    const action$ = this.selectedProjectId() ? this.projectService.updateProject(this.selectedProjectId(), request) : this.projectService.createProject(request);

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          void this.alerts.success(
            this.selectedProjectId() ? 'Project updated' : 'Project created',
            this.selectedProjectId() ? 'The project was updated successfully.' : 'The project was created successfully.'
          );
          this.closeModal();
          this.loadData();
        },
        error: () => {
          this.formError.set('We could not save this project right now. Please try again.');
          void this.alerts.error('Save failed', 'We could not save this project right now. Please try again.');
        }
      });
  }

  async removeProject(id: string): Promise<void> {
    if (!(await this.alerts.confirmDestructive('Delete project?', 'This project will be permanently removed.', 'Delete'))) {
      return;
    }

    this.projectService.deleteProject(id).subscribe({
      next: () => {
        void this.alerts.success('Project deleted', 'The project was deleted successfully.');
        this.loadData();
      },
      error: () => {
        void this.alerts.error('Delete failed', 'We could not delete this project right now. Please try again.');
      }
    });
  }

  private toDateInputValue(value: string | null | undefined): string {
    return value ? value.slice(0, 10) : '';
  }

  formatProjectStartDate(value: string): string {
    return value ? new Date(value).toLocaleDateString() : 'Not selected';
  }
}
