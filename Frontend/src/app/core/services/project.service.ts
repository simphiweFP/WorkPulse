import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { ProjectDetails, ProjectSummary, ProjectUpsertRequest } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  constructor(private readonly http: HttpClient) {}

  getProjects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${apiConfig.apiBaseUrl}/projects`);
  }

  getProject(id: string): Observable<ProjectDetails> {
    return this.http.get<ProjectDetails>(`${apiConfig.apiBaseUrl}/projects/${id}`);
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
}