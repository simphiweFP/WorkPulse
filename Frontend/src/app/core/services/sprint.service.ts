import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { SprintDetails, SprintSummary, SprintUpsertRequest } from '../models/sprint.models';

interface SprintResponse {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: 'Planned' | 'Active' | 'Completed';
  createdAt: string;
  updatedAt: string;
  taskCount: number;
  completedTaskCount: number;
}

@Injectable({ providedIn: 'root' })
export class SprintService {
  constructor(private readonly http: HttpClient) {}

  getSprints(): Observable<SprintSummary[]> {
    return this.http.get<SprintResponse[]>(`${apiConfig.apiBaseUrl}/sprints`).pipe(
      map((sprints) => sprints.map((sprint) => this.toSummary(sprint)))
    );
  }

  getSprint(id: string): Observable<SprintDetails> {
    return this.http.get<SprintResponse>(`${apiConfig.apiBaseUrl}/sprints/${id}`).pipe(
      map((sprint) => this.toDetails(sprint))
    );
  }

  createSprint(request: SprintUpsertRequest): Observable<SprintDetails> {
    return this.http.post<SprintResponse>(`${apiConfig.apiBaseUrl}/sprints`, request).pipe(
      map((sprint) => this.toDetails(sprint))
    );
  }

  updateSprint(id: string, request: SprintUpsertRequest): Observable<SprintDetails> {
    return this.http.put<SprintResponse>(`${apiConfig.apiBaseUrl}/sprints/${id}`, request).pipe(
      map((sprint) => this.toDetails(sprint))
    );
  }

  deleteSprint(id: string): Observable<void> {
    return this.http.delete<void>(`${apiConfig.apiBaseUrl}/sprints/${id}`);
  }

  private toSummary(sprint: SprintResponse): SprintSummary {
    return {
      id: sprint.id,
      name: sprint.name,
      startDate: sprint.startDate,
      endDate: sprint.endDate,
      status: sprint.status,
      taskCount: sprint.taskCount,
      completedTaskCount: sprint.completedTaskCount
    };
  }

  private toDetails(sprint: SprintResponse): SprintDetails {
    return {
      ...this.toSummary(sprint),
      createdAt: sprint.createdAt,
      updatedAt: sprint.updatedAt
    };
  }
}
