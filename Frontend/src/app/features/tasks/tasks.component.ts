import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, forkJoin, map, of } from 'rxjs';
import { ClientSummary } from '../../core/models/client.models';
import { ProjectSummary } from '../../core/models/project.models';
import { TaskAdminSummary, TaskAssigneeOption, TaskUpsertRequest } from '../../core/models/task-admin.models';
import { ClientService } from '../../core/services/client.service';
import { ProjectService } from '../../core/services/project.service';
import { TaskFilter, TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';
import { SprintService } from '../../core/services/sprint.service';
import { SprintSummary } from '../../core/models/sprint.models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    PageHeaderComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PriorityBadgeComponent,
    StatusBadgeComponent
  ],
  styleUrl: './tasks.component.scss',
  templateUrl: './tasks.component.html'
})
export class TasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly clientService = inject(ClientService);
  private readonly projectService = inject(ProjectService);
  private readonly sprintService = inject(SprintService);
  private readonly fb = inject(FormBuilder);

  readonly tasks = signal<TaskAdminSummary[]>([]);
  readonly clients = signal<ClientSummary[]>([]);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly availableProjects = signal<ProjectSummary[]>([]);
  readonly sprints = signal<SprintSummary[]>([]);
  readonly assignees = signal<TaskAssigneeOption[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly selectedTaskId = signal('');
  readonly selectedTask = signal<TaskAdminSummary | null>(null);
  readonly taskFormOpen = signal(false);
  readonly modalActionBusy = signal<'start' | 'complete' | 'delete' | null>(null);
  readonly filtersOpen = signal(true);

  readonly summary = computed(() => {
    const tasks = this.tasks();
    return {
      total: tasks.length,
      todo: tasks.filter((task) => task.status === 'Todo').length,
      inProgress: tasks.filter((task) => task.status === 'InProgress').length,
      completed: tasks.filter((task) => task.status === 'Completed').length
    };
  });

  readonly filters = this.fb.group({
    clientId: [''],
    projectId: [''],
    assigneeId: [''],
    priority: [''],
    status: [''],
    deadline: ['']
  });

  readonly form = this.fb.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    clientId: ['', Validators.required],
    projectId: ['', Validators.required],
    sprintId: [''],
    priority: ['Medium', Validators.required],
    deadline: ['', Validators.required],
    assigneeId: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    unlockBodyScroll();
  }

  activeFilterCount(): number {
    const filters = this.filters.getRawValue();
    return Object.values(filters).filter(Boolean).length;
  }

  private mapTaskDeadline(task: TaskAdminSummary & { dueDate?: string }): TaskAdminSummary {
    return {
      ...task,
      deadline: task.deadline ?? task.dueDate ?? ''
    };
  }

  private loadProjectsForClient(clientId: string): void {
    const request$ = clientId ? this.projectService.getProjectsByClient(clientId) : of(this.projects());

    request$.pipe(
      catchError(() => {
        this.error.set('We could not load the projects for the selected client. Please try again.');
        return of([] as ProjectSummary[]);
      })
    ).subscribe((projects) => this.availableProjects.set(projects));
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');
    let loadFailed = false;
    forkJoin({
      tasks: this.taskService.getTasks(this.filters.getRawValue() as TaskFilter).pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as TaskAdminSummary[]);
        })
      ),
      clients: this.clientService.getClients().pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as ClientSummary[]);
        })
      ),
      projects: this.projectService.getProjects().pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as ProjectSummary[]);
        })
      ),
      sprints: this.sprintService.getSprints().pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as SprintSummary[]);
        })
      ),
      assignees: this.taskService.getDevelopers().pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as TaskAssigneeOption[]);
        })
      )
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ tasks, clients, projects, sprints, assignees }) => {
        this.tasks.set(tasks.map((task) => this.mapTaskDeadline(task)));
        this.clients.set(clients);
        this.projects.set(projects);
        this.availableProjects.set(projects);
        this.sprints.set(sprints);
        this.assignees.set(assignees);
        if (loadFailed) {
          this.error.set('Some task information could not be loaded. Showing what is available.');
        }
      });
  }

  applyFilters(): void {
    this.error.set('');
    this.taskService
      .getTasks(this.filters.getRawValue() as TaskFilter)
      .pipe(
        catchError(() => {
          this.error.set('We could not refresh the task list. Please try again.');
          return of([] as TaskAdminSummary[]);
        })
      )
      .subscribe((tasks) => this.tasks.set(tasks.map((task) => this.mapTaskDeadline(task))));
  }

  resetFilters(): void {
    this.filters.reset();
    this.loadData();
  }

  onClientChange(): void {
    this.form.controls.projectId.setValue('');
    const clientId = this.form.controls.clientId.value;
    this.loadProjectsForClient(clientId ?? '');
  }

  openCreateTask(): void {
    this.closeTaskModal();
    this.selectedTaskId.set('');
    this.form.reset({ priority: 'Medium' });
    this.formError.set('');
    this.taskFormOpen.set(true);
    lockBodyScroll();
  }

  openEditTask(task: TaskAdminSummary): void {
    this.closeTaskModal();
    this.selectedTaskId.set(task.id);
    this.form.patchValue({
      title: task.title,
      description: '',
      clientId: task.clientId,
      projectId: task.projectId,
      sprintId: (task.sprintId ?? ''),
      priority: task.priority,
      deadline: task.deadline,
      assigneeId: this.assignees().find((assignee) => `${assignee.firstName} ${assignee.lastName}` === task.assigneeName)?.id ?? ''
    });
    this.loadProjectsForClient(task.clientId);
    this.formError.set('');
    this.taskFormOpen.set(true);
    lockBodyScroll();
  }

  sprintLabel(task: TaskAdminSummary): string {
    return task.sprintName?.trim() ? task.sprintName : 'Backlog';
  }

  closeTaskForm(): void {
    this.taskFormOpen.set(false);
    this.selectedTaskId.set('');
    this.form.reset({ priority: 'Medium' });
    this.formError.set('');
    unlockBodyScroll();
  }

  openTaskModal(task: TaskAdminSummary): void {
    this.closeTaskForm();
    this.selectedTask.set(task);
    lockBodyScroll();
  }

  closeTaskModal(): void {
    this.selectedTask.set(null);
    this.modalActionBusy.set(null);
    unlockBodyScroll();
  }

  saveTask(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required task fields.');
      return;
    }

    this.submitting.set(true);
    this.error.set('');
    this.formError.set('');
    const request = this.form.getRawValue() as TaskUpsertRequest;
    const action$ = this.selectedTaskId()
      ? this.taskService.updateTask(this.selectedTaskId(), request).pipe(map(() => void 0))
      : this.taskService.createTask(request).pipe(map(() => void 0));

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          void Swal.fire(this.getTaskActionAlertConfig(this.selectedTaskId() ? 'update' : 'create', true));
          this.closeTaskForm();
          this.loadData();
        },
        error: () => {
          this.formError.set('We could not save that task right now. Please try again.');
          void Swal.fire(this.getTaskActionAlertConfig(this.selectedTaskId() ? 'update' : 'create', false));
        }
      });
  }

  startTask(id: string): void {
    this.runModalAction('start', 'We could not start that task right now. Please try again.', this.taskService.startTask(id));
  }

  completeTask(id: string): void {
    this.runModalAction('complete', 'We could not complete that task right now. Please try again.', this.taskService.completeTask(id));
  }

  deleteTask(id: string): void {
    this.runModalAction('delete', 'We could not delete that task right now. Please try again.', this.taskService.deleteTask(id));
  }

  private runModalAction(action: 'start' | 'complete' | 'delete', failureMessage: string, request$: { subscribe: (observer: { next: () => void; error: () => void }) => unknown }): void {
    this.modalActionBusy.set(action);
    this.error.set('');

    request$.subscribe({
      next: () => {
        void Swal.fire(this.getTaskActionAlertConfig(action, true));
        this.closeTaskModal();
        this.loadData();
      },
      error: () => {
        this.error.set(failureMessage);
        void Swal.fire(this.getTaskActionAlertConfig(action, false));
        this.modalActionBusy.set(null);
      }
    });
  }

  private getTaskActionAlertConfig(action: 'create' | 'update' | 'start' | 'complete' | 'delete', success: boolean): { icon: 'success' | 'error'; title: string; text: string; confirmButtonColor: string } {
    const labels = {
      create: 'created',
      update: 'updated',
      start: 'started',
      complete: 'completed',
      delete: 'deleted'
    } as const;

    const failures = {
      create: 'We could not save that task right now. Please try again.',
      update: 'We could not save that task right now. Please try again.',
      start: 'We could not start that task right now. Please try again.',
      complete: 'We could not complete that task right now. Please try again.',
      delete: 'We could not delete that task right now. Please try again.'
    } as const;

    return success
      ? {
          icon: 'success',
          title: `Task ${labels[action]}`,
          text: `The task was ${labels[action]} successfully.`,
          confirmButtonColor: '#4f46e5'
        }
      : {
          icon: 'error',
          title: `Task ${action} failed`,
          text: failures[action],
          confirmButtonColor: '#4f46e5'
        };
  }

}
