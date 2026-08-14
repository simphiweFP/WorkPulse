import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary } from '../../core/models/client.models';
import { ProjectSummary } from '../../core/models/project.models';
import { TaskAdminSummary, TaskAssigneeOption, TaskUpsertRequest } from '../../core/models/task-admin.models';
import { ClientService } from '../../core/services/client.service';
import { ProjectService } from '../../core/services/project.service';
import { TaskFilter, TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      <app-page-header eyebrow="Tasks" title="Tasks" subtitle="Filter, assign, and manage operational work across clients and projects." />

      @if (loading()) {
        <app-loading-state message="Loading tasks..." />
      } @else {
        <section class="panel filters form-grid" [formGroup]="filters">
          <label><input placeholder="Client" formControlName="clientId" /></label>
          <label><input placeholder="Project" formControlName="projectId" /></label>
          <label><input placeholder="Assignee" formControlName="assigneeId" /></label>
          <label>
            <select formControlName="priority">
              <option value="">Priority</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
          </label>
          <label>
            <select formControlName="status">
              <option value="">Status</option>
              <option value="Todo">Todo</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </label>
          <label><input type="date" formControlName="deadline" /></label>
          <div class="actions full-width">
            <button type="button" class="secondary" (click)="applyFilters()">Apply Filters</button>
            <button type="button" class="secondary" (click)="resetFilters()">Reset</button>
          </div>
        </section>

        <form class="panel form-grid" [formGroup]="form" (ngSubmit)="saveTask()">
          <label class="full-width"><input placeholder="Title" formControlName="title" /></label>
          <label class="full-width"><textarea rows="4" placeholder="Description" formControlName="description"></textarea></label>
          <label>
            <select formControlName="clientId" (change)="onClientChange()">
              <option value="">Client</option>
              @for (client of clients(); track client.id) {
                <option [value]="client.id">{{ client.name }}</option>
              }
            </select>
          </label>
          <label>
            <select formControlName="projectId">
              <option value="">Project</option>
              @for (project of filteredProjects(); track project.id) {
                <option [value]="project.id">{{ project.name }}</option>
              }
            </select>
          </label>
          <label>
            <select formControlName="priority">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
          </label>
          <label><input type="date" formControlName="deadline" /></label>
          <label>
            <select formControlName="assigneeId">
              <option value="">Assignee</option>
              @for (assignee of assignees(); track assignee.id) {
                <option [value]="assignee.id">{{ assignee.firstName }} {{ assignee.lastName }}</option>
              }
            </select>
          </label>
          <div class="actions full-width">
            <button type="submit" [disabled]="submitting()">{{ selectedTaskId() ? 'Update' : 'Create' }} Task</button>
            @if (selectedTaskId()) {
              <button type="button" class="secondary" (click)="resetForm()">Cancel</button>
            }
          </div>
        </form>

        @if (tasks().length) {
          <div class="table-card">
            <table>
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Client</th>
                  <th>Project</th>
                  <th>Assignee</th>
                  <th>Priority</th>
                  <th>Deadline</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (task of tasks(); track task.id) {
                  <tr>
                    <td>{{ task.title }}</td>
                    <td>{{ task.clientName }}</td>
                    <td>{{ task.projectName }}</td>
                    <td>{{ task.assigneeName }}</td>
                    <td>{{ task.priority }}</td>
                    <td>{{ task.deadline | date:'mediumDate' }}</td>
                    <td>{{ task.status }}</td>
                    <td class="row-actions">
                      <button type="button" class="secondary" (click)="selectTask(task)">Edit</button>
                      <button type="button" class="secondary" (click)="startTask(task.id)" [disabled]="task.status !== 'Todo'">Start</button>
                      <button type="button" class="secondary" (click)="completeTask(task.id)" [disabled]="task.status === 'Completed'">Complete</button>
                      <button type="button" class="danger" (click)="deleteTask(task.id)">Delete</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <app-empty-state title="No tasks yet." message="Create the first task to start assigning work." />
        }
      }
    </section>
  `
})
export class TasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly clientService = inject(ClientService);
  private readonly projectService = inject(ProjectService);
  private readonly fb = inject(FormBuilder);

  readonly tasks = signal<TaskAdminSummary[]>([]);
  readonly clients = signal<ClientSummary[]>([]);
  readonly projects = signal<ProjectSummary[]>([]);
  readonly assignees = signal<TaskAssigneeOption[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly selectedTaskId = signal('');

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
    priority: ['Medium', Validators.required],
    deadline: ['', Validators.required],
    assigneeId: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  filteredProjects(): ProjectSummary[] {
    const clientId = this.form.controls.clientId.value;
    if (!clientId) {
      return this.projects();
    }

    const clientName = this.clients().find((client) => client.id === clientId)?.name;
    return this.projects().filter((project) => project.clientName === clientName);
  }

  loadData(): void {
    this.loading.set(true);
    this.taskService.getTasks(this.filters.getRawValue() as TaskFilter).subscribe((tasks) => this.tasks.set(tasks));
    this.clientService.getClients().subscribe((clients) => this.clients.set(clients));
    this.projectService.getProjects().subscribe((projects) => this.projects.set(projects));
    this.assignees.set([]);
    this.loading.set(false);
  }

  applyFilters(): void {
    this.taskService.getTasks(this.filters.getRawValue() as TaskFilter).subscribe((tasks) => this.tasks.set(tasks));
  }

  resetFilters(): void {
    this.filters.reset();
    this.loadData();
  }

  onClientChange(): void {
    this.form.controls.projectId.setValue('');
  }

  selectTask(task: TaskAdminSummary): void {
    this.selectedTaskId.set(task.id);
    this.form.patchValue({
      title: task.title,
      description: '',
      clientId: '',
      projectId: '',
      priority: task.priority,
      deadline: task.deadline,
      assigneeId: ''
    });
  }

  resetForm(): void {
    this.selectedTaskId.set('');
    this.form.reset({ priority: 'Medium' });
  }

  saveTask(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const request = this.form.getRawValue() as TaskUpsertRequest;
    const action$ = this.selectedTaskId()
      ? this.taskService.updateTask(this.selectedTaskId(), request)
      : this.taskService.createTask(request);

    action$.pipe(finalize(() => this.submitting.set(false))).subscribe(() => {
      this.resetForm();
      this.loadData();
    });
  }

  startTask(id: string): void {
    this.taskService.startTask(id).subscribe(() => this.loadData());
  }

  completeTask(id: string): void {
    this.taskService.completeTask(id).subscribe(() => this.loadData());
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id).subscribe(() => this.loadData());
  }
}