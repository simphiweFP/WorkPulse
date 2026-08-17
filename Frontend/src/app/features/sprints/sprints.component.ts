import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ProjectSummary } from '../../core/models/project.models';
import { SprintService } from '../../core/services/sprint.service';
import { ProjectService } from '../../core/services/project.service';
import { SprintDetails, SprintSummary, SprintUpsertRequest } from '../../core/models/sprint.models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';
import { TaskAdminSummary, TaskAssigneeOption, TaskUpsertRequest } from '../../core/models/task-admin.models';
import { TaskService } from '../../core/services/task.service';

@Component({
  selector: 'app-sprints',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './sprints.component.scss',
  templateUrl: './sprints.component.html'
})
export class SprintsComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly sprintService = inject(SprintService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly alerts = inject(FeedbackAlertService);

  readonly sprints = signal<SprintSummary[]>([]);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly availableProjects = signal<ProjectSummary[]>([]);
  readonly selectedSprint = signal<SprintDetails | null>(null);
  readonly taskSprint = signal<SprintSummary | null>(null);
  readonly sprintTaskDrawerOpen = signal(false);
  readonly sprintTaskDrawerReadOnly = signal(false);
  readonly sprintTasks = signal<TaskAdminSummary[]>([]);
  readonly availableSprintTasks = signal<TaskAdminSummary[]>([]);
  readonly taskAssignees = signal<TaskAssigneeOption[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly formError = signal('');
  readonly sprintTaskError = signal('');
  readonly sprintTaskWarning = signal('');
  readonly sprintTaskSubmitting = signal(false);
  readonly sprintTaskLoading = signal(false);
  readonly modalOpen = signal(false);
  readonly selectedSprintId = signal('');
  readonly submitting = signal(false);
  readonly selectedSprintTaskId = signal('');

  readonly selectedSprintProject = computed(() => {
    const sprint = this.selectedSprint();
    return sprint ? this.projects().find((project) => project.id === sprint.projectId) ?? null : null;
  });

  readonly form = this.fb.group({
    projectId: ['', Validators.required],
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    status: ['Planned', Validators.required],
    totalTasks: [0, [Validators.required, Validators.min(0)]]
  });

  readonly taskSelectionForm = this.fb.group({
    taskId: ['', Validators.required]
  });
  readonly canManageSprints = computed(() => this.authService.getCurrentUserSnapshot()?.role === 'Admin');
  readonly canManageSprintTasks = computed(() => this.canManageSprints());
  readonly canViewAllSprints = computed(() => this.canManageSprints());

  canManageSprintsView(): boolean {
    return this.canManageSprints();
  }

  canManageSprintTasksView(): boolean {
    return this.canManageSprintTasks();
  }

  canViewAllSprintsView(): boolean {
    return this.canViewAllSprints();
  }

  sprintTaskDrawerReadOnlyView(): boolean {
    return this.sprintTaskDrawerReadOnly();
  }

  taskSelectionFormView = this.taskSelectionForm;

  availableSprintTasksView(): TaskAdminSummary[] {
    return this.availableSprintTasks();
  }

  sprintProgressPercentView(sprint: SprintSummary): number {
    return this.sprintProgressPercent(sprint);
  }

  sprintProgressLabelView(sprint: SprintSummary): string {
    return this.sprintProgressLabel(sprint);
  }

  ngOnInit(): void {
    this.loadSprints();
    this.taskSelectionForm.controls.taskId.valueChanges.subscribe(() => {
      if (this.sprintTaskDrawerOpen()) {
        this.sprintTaskWarning.set(this.getSprintTaskDisableReason());
      }
    });
  }

  loadSprints(): void {
    this.loading.set(true);
    this.error.set('');

    this.projectService
      .getProjects()
      .pipe(catchError(() => of([] as ProjectSummary[])))
      .subscribe((projects) => {
        this.projects.set(projects);
        this.availableProjects.set(projects);
      });

    this.taskService
      .getDevelopers()
      .pipe(catchError(() => of([] as TaskAssigneeOption[])))
      .subscribe((assignees) => this.taskAssignees.set(assignees));

    const sprints$ = this.sprintService.getSprints().pipe(
      catchError(() => {
        this.error.set('Unable to load sprint data right now.');
        return of([] as SprintSummary[]);
      })
    );

    if (this.canViewAllSprints()) {
      sprints$
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((sprints) => {
          this.sprints.set(sprints);
        });
      return;
    }

    this.taskService
      .getMyTasks()
      .pipe(
        catchError(() => of([] as TaskAdminSummary[])),
        finalize(() => this.loading.set(false))
      )
      .subscribe((tasks) => {
        const sprintIds = new Set(tasks.map((task) => task.sprintId).filter((value): value is string => !!value));
        sprints$.subscribe((sprints) => {
          this.sprints.set(sprints.filter((sprint) => sprintIds.has(sprint.id)));
        });
      });
  }

  completionPercent(): number {
    const sprint = this.selectedSprint();
    if (!sprint) {
      return 0;
    }

    if (sprint.totalPoints <= 0) {
      return 0;
    }

    return Math.round((sprint.completedPoints / sprint.totalPoints) * 100);
  }

  sprintProgressPercent(sprint: SprintSummary): number {
    if (sprint.totalPoints <= 0) {
      return 0;
    }

    return Math.round((sprint.completedPoints / sprint.totalPoints) * 100);
  }

  sprintProgressLabel(sprint: SprintSummary): string {
    return `${sprint.completedPoints}/${sprint.totalPoints} pts completed`;
  }

  openCreateSprint(): void {
    if (!this.canManageSprints()) {
      return;
    }
    this.selectedSprintId.set('');
    this.form.reset({ status: 'Planned', totalTasks: 0 });
    this.availableProjects.set(this.projects());
    this.formError.set('');
    this.closeSprintTaskDrawer();
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  selectSprint(sprint: SprintSummary): void {
    if (!this.canManageSprints()) {
      return;
    }
    this.selectedSprintId.set(sprint.id);
    this.sprintService.getSprint(sprint.id).subscribe((details) => {
      this.selectedSprint.set(details);
      this.form.patchValue({
        projectId: details.projectId,
        name: details.name,
        startDate: this.toDateInput(details.startDate),
        endDate: this.toDateInput(details.endDate),
        status: details.status,
        totalTasks: details.totalTasks
      });
      this.availableProjects.set(this.projects());
      this.formError.set('');
      this.closeSprintTaskDrawer();
      this.modalOpen.set(true);
      lockBodyScroll();
    });
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.selectedSprintId.set('');
    this.selectedSprint.set(null);
    this.form.reset({ status: 'Planned', totalTasks: 0 });
    this.availableProjects.set(this.projects());
    this.formError.set('');
    this.closeSprintTaskDrawer();
    unlockBodyScroll();
  }

  openSprintTaskDrawer(sprint: SprintSummary): void {
    const readOnly = !this.canManageSprintTasks();
    this.closeModal();
    this.taskSprint.set(sprint);
    this.sprintTaskDrawerReadOnly.set(readOnly);
    this.sprintTaskError.set('');
    this.sprintTaskWarning.set('');
    this.taskSelectionForm.reset({ taskId: '' });
    this.sprintTaskDrawerOpen.set(true);
    this.loadSprintTasks(sprint);
    this.refreshSprintTaskWarning(sprint);
    lockBodyScroll();
  }

  closeSprintTaskDrawer(): void {
    this.sprintTaskDrawerOpen.set(false);
    this.sprintTaskDrawerReadOnly.set(false);
    this.taskSprint.set(null);
    this.sprintTasks.set([]);
    this.sprintTaskError.set('');
    this.sprintTaskWarning.set('');
    this.sprintTaskLoading.set(false);
    this.sprintTaskSubmitting.set(false);
    this.taskSelectionForm.reset({ taskId: '' });
    unlockBodyScroll();
  }

  startSprintTask(task: TaskAdminSummary): void {
    if (!this.canManageSprintTasks()) {
      return;
    }
    void this.confirmAndRunSprintTaskAction(task, 'start', 'Start task?', 'This will move the task into progress.', 'Start', 'We could not start that task right now. Please try again.', () => this.taskService.startTask(task.id));
  }

  completeSprintTask(task: TaskAdminSummary): void {
    if (!this.canManageSprintTasks()) {
      return;
    }
    void this.confirmAndRunSprintTaskAction(task, 'complete', 'Complete task?', 'This will mark the task as completed.', 'Complete', 'We could not complete that task right now. Please try again.', () => this.taskService.completeTask(task.id));
  }

  async removeSprintTask(task: TaskAdminSummary): Promise<void> {
    if (!this.canManageSprintTasks()) {
      return;
    }
    if (!(await this.alerts.confirmDestructive('Remove from sprint?', 'The task will remain in the project backlog.', 'Remove'))) {
      return;
    }

    const sprint = this.taskSprint();
    const project = sprint ? this.projects().find((item) => item.id === sprint.projectId) ?? null : null;

    if (!sprint || !project) {
      this.sprintTaskError.set('We could not determine the sprint project. Please try again.');
      return;
    }

    const request: TaskUpsertRequest = {
      title: task.title,
      description: task.description ?? '',
      clientId: project.clientId,
      projectId: task.projectId,
      sprintId: null,
      type: task.type,
      priority: task.priority,
      status: task.status ?? 'Todo',
      storyPoints: task.storyPoints,
      deadline: task.deadline,
      assigneeId: task.assigneeId ?? ''
    };

    this.runSprintTaskAction(task, 'remove', 'We could not remove that task from this sprint right now. Please try again.', () => this.taskService.updateTask(task.id, request));
  }

  saveSprintTask(): void {
    if (!this.canManageSprintTasks() || this.sprintTaskDrawerReadOnly()) {
      return;
    }
    if (this.taskSelectionForm.invalid) {
      this.taskSelectionForm.markAllAsTouched();
      this.sprintTaskError.set('Please select an existing task.');
      return;
    }

    const disableReason = this.getSprintTaskDisableReason();
    if (disableReason) {
      this.sprintTaskError.set(disableReason);
      return;
    }

    const sprint = this.taskSprint();
    const project = sprint ? this.projects().find((item) => item.id === sprint.projectId) ?? null : null;
    const taskId = this.taskSelectionForm.controls.taskId.value;
    const task = taskId ? this.availableSprintTasks().find((item) => item.id === taskId) ?? null : null;

    if (!sprint || !project || !task) {
      this.sprintTaskError.set('We could not determine the selected task or sprint project. Please try again.');
      return;
    }

    this.sprintTaskSubmitting.set(true);
    this.sprintTaskError.set('');
    const request: TaskUpsertRequest = {
      title: task.title,
      description: task.description ?? '',
      clientId: project.clientId,
      projectId: task.projectId,
      sprintId: sprint.id,
      type: task.type,
      priority: task.priority,
      status: task.status ?? 'Todo',
      storyPoints: task.storyPoints,
      deadline: task.deadline,
      assigneeId: task.assigneeId ?? ''
    };

    const action$ = this.taskService.updateTask(task.id, request);

    action$
      .pipe(finalize(() => this.sprintTaskSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.taskSelectionForm.reset({ taskId: '' });
          this.loadSprintTasks(sprint);
          void this.alerts.success('Task assigned to sprint', `"${task.title}" was added to "${sprint.name}".`);
        },
        error: () => {
          this.sprintTaskError.set('We could not save that task right now. Please try again.');
          void this.alerts.error('Save failed', 'We could not save that task right now. Please try again.');
        }
      });
  }

  sprintTaskLabel(task: TaskAdminSummary): string {
    return task.sprintName?.trim() ? task.sprintName : 'Backlog';
  }

  saveSprint(): void {
    if (!this.canManageSprints()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required sprint fields.');
      return;
    }

    const request = this.form.getRawValue() as SprintUpsertRequest;
    this.submitting.set(true);
    this.formError.set('');
    const action$ = this.selectedSprintId()
      ? this.sprintService.updateSprint(this.selectedSprintId(), request)
      : this.sprintService.createSprint(request);

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          void this.alerts.success(
            this.selectedSprintId() ? 'Sprint updated' : 'Sprint created',
            this.selectedSprintId() ? 'The sprint was updated successfully.' : 'The sprint was created successfully.'
          );
          this.closeModal();
          this.loadSprints();
        },
        error: () => {
          this.formError.set('We could not save this sprint right now. Please try again.');
          void this.alerts.error('Save failed', 'We could not save this sprint right now. Please try again.');
        }
      });
  }

  async removeSprint(id: string): Promise<void> {
    if (!this.canManageSprints()) {
      return;
    }
    if (!(await this.alerts.confirmDestructive('Delete sprint?', 'This sprint will be permanently removed.', 'Delete'))) {
      return;
    }

    this.sprintService.deleteSprint(id).subscribe({
      next: () => {
        void this.alerts.success('Sprint deleted', 'The sprint was deleted successfully.');
        this.loadSprints();
      },
      error: () => {
        void this.alerts.error('Delete failed', 'We could not delete this sprint right now. Please try again.');
      }
    });
  }

  private async confirmAndRunSprintTaskAction(
    task: TaskAdminSummary,
    action: 'start' | 'complete',
    title: string,
    text: string,
    confirmButtonText: string,
    failureMessage: string,
    requestFactory: () => { subscribe: (observer: { next: () => void; error: () => void }) => unknown }
  ): Promise<void> {
    if (!(await this.alerts.confirmAction(title, text, confirmButtonText))) {
      return;
    }

    this.runSprintTaskAction(task, action, failureMessage, requestFactory);
  }

  private loadSprintTasks(sprint: SprintSummary): void {
    this.sprintTaskLoading.set(true);
    this.sprintTaskError.set('');

    this.taskService
      .getTasks({ projectId: sprint.projectId })
      .pipe(
        catchError(() => {
          this.sprintTaskError.set('We could not load tasks for this sprint right now.');
          return of([] as TaskAdminSummary[]);
        }),
        finalize(() => this.sprintTaskLoading.set(false))
      )
      .subscribe((tasks) => {
        this.sprintTasks.set(tasks.filter((task) => task.sprintId === sprint.id));
        this.availableSprintTasks.set(tasks.filter((task) => task.projectId === sprint.projectId && task.status !== 'Completed'));
      });
  }

  private runSprintTaskAction(
    task: TaskAdminSummary,
    action: 'start' | 'complete' | 'remove',
    failureMessage: string,
    requestFactory: () => { subscribe: (observer: { next: () => void; error: () => void }) => unknown }
  ): void {
    this.sprintTaskError.set('');

    requestFactory().subscribe({
      next: () => {
        if (this.taskSprint()) {
          this.loadSprintTasks(this.taskSprint()!);
          this.refreshSprintTaskWarning(this.taskSprint()!);
        }

        this.taskSelectionForm.reset({ taskId: '' });
        if (action === 'remove') {
          void this.alerts.success('Removed from sprint', `"${task.title}" was removed from this sprint and kept in the project backlog.`);
          return;
        }

        const messages = {
          start: ['Task started', `"${task.title}" was started successfully.`],
          complete: ['Task completed', `"${task.title}" was completed successfully.`]
        } as const;
        const [title, text] = messages[action];
        void this.alerts.success(title, text);
      },
      error: () => {
        this.sprintTaskError.set(failureMessage);
        const messages = {
          start: ['Start failed', 'We could not start that task right now. Please try again.'],
          complete: ['Complete failed', 'We could not complete that task right now. Please try again.'],
          remove: ['Remove failed', 'We could not remove that task from this sprint right now. Please try again.']
        } as const;
        const [title, text] = messages[action];
        void this.alerts.error(title, text);
      }
    });
  }

  private refreshSprintTaskWarning(sprint?: SprintSummary | null): void {
    if (!sprint) {
      this.sprintTaskWarning.set('');
      return;
    }

    const reason = this.getSprintTaskDisableReason();
    if (reason) {
      this.sprintTaskWarning.set(reason);
      return;
    }

    const totalStoryPoints = this.sprintTasks().filter((task) => task.sprintId === sprint.id).reduce((sum, task) => sum + (task.storyPoints ?? 0), 0);

    if (sprint.totalTasks <= 0) {
      this.sprintTaskWarning.set('Sprint point is zero, so you cannot add tasks until sprint point is increased.');
      return;
    }

    if (totalStoryPoints >= sprint.totalTasks) {
      this.sprintTaskWarning.set('The sprint point capacity is full for the current story points. Please adjust the sprint point or task estimates.');
      return;
    }

    this.sprintTaskWarning.set('');
  }

  sprintTaskDisableReason(): string {
    return this.getSprintTaskDisableReason();
  }

  private getSprintTaskDisableReason(): string {
    const sprint = this.taskSprint();
    if (!sprint) {
      return 'We could not determine the sprint project. Please try again.';
    }

    const taskId = this.taskSelectionForm.controls.taskId.value;
    const task = taskId ? this.availableSprintTasks().find((item) => item.id === taskId) ?? null : null;
    const storyPoints = Number(task?.storyPoints ?? 0);
    const totalStoryPoints = this.sprintTasks().reduce((sum, task) => sum + (task.storyPoints ?? 0), 0);

    if (sprint.totalTasks <= 0) {
      return 'Sprint point is zero, so you cannot add tasks until sprint point is increased.';
    }

    if (storyPoints > sprint.totalTasks) {
      return 'This task story points are greater than the sprint point capacity.';
    }

    if (totalStoryPoints + storyPoints > sprint.totalTasks) {
      return 'Adding this task would exceed the sprint point capacity.';
    }

    return '';
  }

  onClientChange(): void {
    this.form.controls.projectId.reset('');
    this.availableProjects.set(this.projects());
  }

  projectName(projectId: string): string {
    return this.projects().find((project) => project.id === projectId)?.name ?? 'Unknown project';
  }

  private toDateInput(value: string): string {
    return new Date(value).toISOString().slice(0, 10);
  }
}
