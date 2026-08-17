import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, forkJoin, map, of } from 'rxjs';
import { ClientSummary } from '../../core/models/client.models';
import { ProjectSummary } from '../../core/models/project.models';
import { TaskAdminSummary, TaskAssigneeOption, TaskUpsertRequest } from '../../core/models/task-admin.models';
import { TaskType, TaskPriority, TaskStatus } from '../../shared/models/task.models';
import { ClientService } from '../../core/services/client.service';
import { ProjectService } from '../../core/services/project.service';
import { TaskFilter, TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
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
  private readonly fb = inject(FormBuilder);
  private readonly alerts = inject(FeedbackAlertService);

  readonly tasks = signal<TaskAdminSummary[]>([]);
  readonly clients = signal<ClientSummary[]>([]);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly availableProjects = signal<ProjectSummary[]>([]);
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
    type: ['', Validators.required],
    storyPoints: [1, [Validators.required, Validators.min(1)]],
    sprintId: [''],
    priority: ['', Validators.required],
    status: ['', Validators.required],
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
      assignees: this.taskService.getDevelopers().pipe(
        catchError(() => {
          loadFailed = true;
          return of([] as TaskAssigneeOption[]);
        })
      )
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ tasks, clients, projects, assignees }) => {
        this.tasks.set(tasks.map((task) => this.mapTaskDeadline(task)));
        this.clients.set(clients);
        this.projects.set(projects);
        this.syncAvailableProjectsForForm();
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
    const clientId = (this.form.controls.clientId.value ?? '') as string;
    this.loadProjectsForClient(clientId);
  }

  onProjectChange(): void {
    // When project changes, clear sprint selection so user must choose explicitly
    this.form.controls.sprintId.setValue(null);
  }

  private syncAvailableProjectsForForm(): void {
    const clientId = (this.form.controls.clientId.value ?? '') as string;
    if (!clientId) {
      this.availableProjects.set(this.projects());
      return;
    }

    this.loadProjectsForClient(clientId);
  }

  openCreateTask(): void {
    this.closeTaskModal();
    this.selectedTaskId.set('');
    this.form.reset({ priority: '', sprintId: null, storyPoints: 1, status: '', type: '' });
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
      storyPoints: task.storyPoints,
      type: task.type,
      sprintId: task.sprintId ?? null,
      priority: task.priority,
      status: task.status,
      deadline: task.deadline,
      assigneeId: this.assignees().find((assignee) => `${assignee.firstName} ${assignee.lastName}` === task.assigneeName)?.id ?? ''
    });
    this.syncAvailableProjectsForForm();
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
    this.form.reset({ priority: '', sprintId: null, storyPoints: 1, status: '', type: '' });
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

    const raw = this.form.getRawValue();
    const clientId: string = (raw.clientId ?? '') as string;
    const projectId: string = (raw.projectId ?? '') as string;
    const type = (raw.type ?? 'Story') as TaskType;
    const storyPoints = Number(raw.storyPoints ?? 1);
    const priority = (raw.priority ?? 'Medium') as TaskPriority;
    const status = (raw.status ?? 'Todo') as TaskStatus;
    const deadline: string = (raw.deadline ?? '') as string;
    const assigneeId: string = (raw.assigneeId ?? '') as string;

    const request: TaskUpsertRequest = {
      title: (raw.title ?? '') as string,
      description: (raw.description ?? '') as string,
      clientId,
      projectId,
      sprintId: raw.sprintId ?? null,
      type,
      priority,
      status,
      storyPoints,
      // sprintOrder omitted when not present in form
      deadline,
      assigneeId
    };

    const action$ = this.selectedTaskId() ? this.taskService.updateTask(this.selectedTaskId(), request) : this.taskService.createTask(request);

    this.submitting.set(true);
    this.formError.set('');

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          void this.alerts.success(this.selectedTaskId() ? 'Task updated' : 'Task created', this.selectedTaskId() ? 'The task was updated successfully.' : 'The task was created successfully.');
          this.closeTaskForm();
          this.loadData();
        },
        error: () => {
          this.formError.set('We could not save this task right now. Please try again.');
          void this.alerts.error('Save failed', 'We could not save this task right now. Please try again.');
        }
      });
  }

  deleteTask(id: string): void {
    if (!confirm('Delete this task?')) {
      return;
    }

    this.taskService.deleteTask(id).subscribe({
      next: () => {
        void this.alerts.success('Task deleted', 'The task was deleted successfully.');
        this.loadData();
      },
      error: () => {
        void this.alerts.error('Delete failed', 'We could not delete this task right now. Please try again.');
      }
    });
  }

  startTask(id: string): void {
    this.modalActionBusy.set('start');
    this.taskService.startTask(id).subscribe({
      next: () => {
        this.modalActionBusy.set(null);
        this.loadData();
      },
      error: () => {
        this.modalActionBusy.set(null);
        void this.alerts.error('Action failed', 'We could not start this task right now. Please try again.');
      }
    });
  }

  completeTask(id: string): void {
    this.modalActionBusy.set('complete');
    this.taskService.completeTask(id).subscribe({
      next: () => {
        this.modalActionBusy.set(null);
        this.loadData();
      },
      error: () => {
        this.modalActionBusy.set(null);
        void this.alerts.error('Action failed', 'We could not complete this task right now. Please try again.');
      }
    });
  }
}
