import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { TaskActionResponse, TaskPriority, TaskStatus, TaskRecommendation } from '../../shared/models/task.models';
import { TaskAdminSummary, TaskUpsertRequest } from '../models/task-admin.models';

export interface TaskFilter {
  clientId?: string;
  projectId?: string;
  assigneeId?: string;
  priority?: TaskPriority | '';
  status?: TaskStatus | '';
  deadline?: string;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  constructor(private readonly http: HttpClient) {}

  getRecommendedToday(): Observable<TaskRecommendation[]> {
    return this.http.get<TaskRecommendation[]>(`${apiConfig.apiBaseUrl}/tasks/recommendations/today`);
  }

  getMyTasks(): Observable<TaskRecommendation[]> {
    return this.http.get<TaskRecommendation[]>(`${apiConfig.apiBaseUrl}/tasks/my-tasks`);
  }

  getTasks(filters: TaskFilter = {}): Observable<TaskAdminSummary[]> {
    let params = new HttpParams();

    Object.entries(filters).forEach(([key, value]) => {
      if (value) {
        params = params.set(key, value);
      }
    });

    return this.http.get<TaskAdminSummary[]>(`${apiConfig.apiBaseUrl}/tasks`, { params });
  }

  createTask(request: TaskUpsertRequest): Observable<TaskAdminSummary> {
    return this.http.post<TaskAdminSummary>(`${apiConfig.apiBaseUrl}/tasks`, request);
  }

  updateTask(id: string, request: TaskUpsertRequest): Observable<TaskAdminSummary> {
    return this.http.put<TaskAdminSummary>(`${apiConfig.apiBaseUrl}/tasks/${id}`, request);
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${apiConfig.apiBaseUrl}/tasks/${id}`);
  }

  startTask(taskId: string): Observable<TaskActionResponse> {
    return this.http.post<TaskActionResponse>(`${apiConfig.apiBaseUrl}/tasks/${taskId}/start`, {});
  }

  completeTask(taskId: string): Observable<TaskActionResponse> {
    return this.http.post<TaskActionResponse>(`${apiConfig.apiBaseUrl}/tasks/${taskId}/complete`, {});
  }
}