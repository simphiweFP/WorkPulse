import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { TodayDashboardResponse } from '../../shared/models/task.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  getTodayDashboard(): Observable<TodayDashboardResponse> {
    return this.http.get<TodayDashboardResponse>(`${apiConfig.apiBaseUrl}/dashboard/today`);
  }
}