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
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './projects.component.scss',
  templateUrl: './projects.component.html'
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
  readonly error = signal('');
  readonly formError = signal('');
  readonly modalOpen = signal(false);
  readonly search = signal('');

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
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.projects();
    }

    return this.projects().filter((project) => `${project.name} ${project.clientName}`.toLowerCase().includes(term));
  }

  openCreateProject(): void {
    this.selectedProjectId.set('');
    this.form.reset({ status: 'Active' });
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  selectProject(project: ProjectSummary): void {
    this.selectedProjectId.set(project.id);
    this.form.patchValue({
      name: project.name,
      clientId: '',
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
    this.form.reset({ status: 'Active' });
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
      .subscribe(() => {
        this.closeModal();
        this.loadData();
      }, () => {
        this.formError.set('We could not save this project right now. Please try again.');
      });
  }

  removeProject(id: string): void {
    this.projectService.deleteProject(id).subscribe(() => this.loadData());
  }
}
