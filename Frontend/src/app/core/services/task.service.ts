import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { apiConfig } from './api.config';
import { TaskActionResponse, TaskPriority, TaskStatus, TaskRecommendation } from '../../shared/models/task.models';
import { TaskAdminSummary, TaskAssigneeOption, TaskUpsertRequest } from '../models/task-admin.models';

export interface TaskFilter {
  clientId?: string;
  projectId?: string;
  assigneeId?: string;
  priority?: TaskPriority | '';
  status?: TaskStatus | '';
  deadline?: string;
}

interface TaskResponse {
  id: string;
  projectId: string;
  sprintId?: string | null;
  sprintName?: string;
  projectName: string;
  clientId: string;
  clientName: string;
  assignedToUserId?: string | null;
  assignedUserName: string;
  title: string;
  description: string;
  deadline?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  createdAt: string;
  updatedAt: string;
  completedAt?: string | null;
  recommendationReason?: string;
}

interface TodayResponse {
  summary: {
    total: number;
    overdue: number;
    deadlineToday: number;
    highPriority: number;
  };
  tasks: TaskResponse[];
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  constructor(private readonly http: HttpClient) {}

  getRecommendedToday(): Observable<TaskRecommendation[]> {
    return this.http.get<TodayResponse>(`${apiConfig.apiBaseUrl}/tasks/today`).pipe(map((response) => response.tasks.map((task) => this.toRecommendation(task))));
  }

  getMyTasks(filters: TaskFilter = {}): Observable<TaskRecommendation[]> {
    let params = new HttpParams();
    if (filters.status) params = params.set('status', filters.status);
    if (filters.priority) params = params.set('priority', filters.priority);
    if (filters.projectId) params = params.set('projectId', filters.projectId);
    if (filters.deadline) params = params.set('deadline', filters.deadline);
    return this.http.get<TaskResponse[]>(`${apiConfig.apiBaseUrl}/tasks/my`, { params }).pipe(map((tasks) => tasks.map((task) => this.toRecommendation(task))));
  }

  getTasks(filters: TaskFilter = {}): Observable<TaskAdminSummary[]> {
    let params = new HttpParams();
    if (filters.clientId) params = params.set('clientId', filters.clientId);
    if (filters.projectId) params = params.set('projectId', filters.projectId);
    if (filters.assigneeId) params = params.set('assigneeId', filters.assigneeId);
    if (filters.priority) params = params.set('priority', filters.priority);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.deadline) params = params.set('deadline', filters.deadline);
    return this.http.get<TaskResponse[]>(`${apiConfig.apiBaseUrl}/tasks`, { params }).pipe(map((tasks) => tasks.map((task) => this.toAdminSummary(task))));
  }

  getTask(id: string): Observable<TaskResponse> {
    return this.http.get<TaskResponse>(`${apiConfig.apiBaseUrl}/tasks/${id}`);
  }

  createTask(request: TaskUpsertRequest): Observable<TaskAdminSummary> {
    return this.http.post<TaskResponse>(`${apiConfig.apiBaseUrl}/tasks`, {
      projectId: request.projectId,
      sprintId: request.sprintId ?? null,
      title: request.title,
      description: request.description,
      priority: request.priority,
      assignedToUserId: request.assigneeId,
      deadline: request.deadline
    }).pipe(map((task) => this.toAdminSummary(task)));
  }

  updateTask(id: string, request: TaskUpsertRequest): Observable<TaskAdminSummary> {
    return this.http.put<TaskResponse>(`${apiConfig.apiBaseUrl}/tasks/${id}`, {
      projectId: request.projectId,
      sprintId: request.sprintId ?? null,
      title: request.title,
      description: request.description,
      priority: request.priority,
      assignedToUserId: request.assigneeId,
      deadline: request.deadline
    }).pipe(map((task) => this.toAdminSummary(task)));
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${apiConfig.apiBaseUrl}/tasks/${id}`);
  }

  assignTask(id: string, userId: string): Observable<void> {
    return this.http.patch<void>(`${apiConfig.apiBaseUrl}/tasks/${id}/assign`, { assignedToUserId: userId });
  }

  startTask(taskId: string): Observable<TaskActionResponse> {
    return this.http.patch<void>(`${apiConfig.apiBaseUrl}/tasks/${taskId}/status`, { status: 'InProgress' }).pipe(map(() => ({ taskId, status: 'InProgress' as TaskStatus })));
  }

  completeTask(taskId: string): Observable<TaskActionResponse> {
    return this.http.patch<void>(`${apiConfig.apiBaseUrl}/tasks/${taskId}/complete`, {}).pipe(map(() => ({ taskId, status: 'Completed' as TaskStatus })));
  }

  getDevelopers(): Observable<TaskAssigneeOption[]> {
    return this.http.get<TaskAssigneeOption[]>(`${apiConfig.apiBaseUrl}/users/developers`);
  }

  private resolveSprintName(task: TaskResponse): string {
    return task.sprintName ?? (task.sprintId ? 'Sprint' : 'Backlog');
  }

  private toAdminSummary(task: TaskResponse): TaskAdminSummary {
    return {
      id: task.id,
      clientId: task.clientId,
      projectId: task.projectId,
      title: task.title,
      clientName: task.clientName,
      projectName: task.projectName,
      sprintId: task.sprintId,
      sprintName: this.resolveSprintName(task),
      assigneeName: task.assignedUserName,
      priority: task.priority,
      deadline: task.deadline ?? task.createdAt,
      status: task.status
    };
  }

  private toRecommendation(task: TaskResponse): TaskRecommendation {
    return {
      taskId: task.id,
      title: task.title,
      clientName: task.clientName,
      projectName: task.projectName,
      sprintId: task.sprintId,
      sprintName: this.resolveSprintName(task),
      priority: task.priority,
      status: task.status,
      deadline: task.deadline ?? task.createdAt,
      reason: task.recommendationReason ?? '',
      isOverdue: (task.recommendationReason ?? '').toLowerCase().includes('overdue'),
      isDueToday: (task.recommendationReason ?? '').toLowerCase().includes('today'),
      actionLabel: task.status === 'Completed' ? 'View' : task.status === 'InProgress' ? 'Complete' : 'Start'
    };
  }
}
