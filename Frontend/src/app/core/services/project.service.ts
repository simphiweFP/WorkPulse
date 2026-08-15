import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { ProjectDetails, ProjectResponse, ProjectSummary, ProjectUpsertRequest } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  constructor(private readonly http: HttpClient) {}

  getProjects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectResponse[]>(`${apiConfig.apiBaseUrl}/projects`).pipe(
      map((projects) => projects.map((project) => this.toSummary(project)))
    );
  }

  getProject(id: string): Observable<ProjectDetails> {
    return this.http.get<ProjectDetails>(`${apiConfig.apiBaseUrl}/projects/${id}`);
  }

  getProjectsByClient(clientId: string): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${apiConfig.apiBaseUrl}/clients/${clientId}/projects`);
  }

  createProject(request: ProjectUpsertRequest): Observable<ProjectDetails> {
    return this.http.post<ProjectDetails>(`${apiConfig.apiBaseUrl}/projects`, request);
  }

  updateProject(id: string, request: ProjectUpsertRequest): Observable<ProjectDetails> {
    return this.http.put<ProjectDetails>(`${apiConfig.apiBaseUrl}/projects/${id}`, request);
  }

  deleteProject(id: string): Observable<void> {
    return this.http.delete<void>(`${apiConfig.apiBaseUrl}/projects/${id}`);
  }

  private toSummary(project: ProjectResponse): ProjectSummary {
    return {
      id: project.id,
      clientId: project.clientId,
      name: project.name,
      clientName: project.clientName,
      status: project.status,
      openTasks: project.openTaskCount,
      completedTasks: project.completedTaskCount
    };
  }
}
