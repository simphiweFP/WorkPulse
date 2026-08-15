import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { TaskPriority, TaskRecommendation, TaskStatus, TodayDashboardResponse } from '../../shared/models/task.models';

interface BackendTodayResponse {
  summary: {
    total: number;
    overdue: number;
    deadlineToday: number;
    highPriority: number;
  };
  tasks: Array<{
    id: string;
    projectId: string;
    projectName: string;
    clientId: string;
    clientName: string;
    title: string;
    description: string;
    deadline?: string | null;
    status: TaskStatus;
    priority: TaskPriority;
    recommendationReason: string;
    score: number;
  }>;
}

export interface AdminDashboardResponse {
  summary: {
    clients: number;
    projects: number;
    tasks: number;
    teamMembers: number;
  };
  taskOverview: {
    overdue: number;
    dueToday: number;
    inProgress: number;
    completed: number;
  };
  priorityBreakdown: {
    critical: number;
    high: number;
    medium: number;
    low: number;
  };
  recentTasks: Array<{
    id: string;
    title: string;
    projectName: string;
    clientName: string;
    assigneeName: string;
    priority: TaskPriority;
    deadline?: string | null;
    status: TaskStatus;
  }>;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  getTodayDashboard(): Observable<TodayDashboardResponse> {
    return this.http.get<BackendTodayResponse>(`${apiConfig.apiBaseUrl}/tasks/today`).pipe(
      map((response) => this.toTodayDashboard(response))
    );
  }

  getAdminDashboard(): Observable<AdminDashboardResponse> {
    return this.http.get<AdminDashboardResponse>(`${apiConfig.apiBaseUrl}/dashboard/admin`);
  }

  private toTodayDashboard(response: BackendTodayResponse): TodayDashboardResponse {
    const recommendations = response.tasks.map((task) => this.toRecommendation(task));
    const overdue = recommendations.filter((task) => task.isOverdue);
    const dueToday = recommendations.filter((task) => task.isDueToday);
    const completedToday: TaskRecommendation[] = [];
    const remaining = recommendations.filter((task) => !task.isOverdue && !task.isDueToday);

    return {
      date: new Date().toISOString(),
      summary: {
        tasksToday: response.summary.total,
        overdue: response.summary.overdue,
        deadlineToday: response.summary.deadlineToday,
        highPriority: response.summary.highPriority
      },
      topPriority: recommendations[0] ?? this.emptyRecommendation(),
      overdue,
      dueToday,
      recommendedNext: remaining,
      completedToday
    };
  }

  private toRecommendation(task: BackendTodayResponse['tasks'][number]): TaskRecommendation {
    const deadline = task.deadline ?? new Date().toISOString();
    const reason = task.recommendationReason || this.defaultReason(task.priority, task.deadline);
    return {
      taskId: task.id,
      title: task.title,
      clientName: task.clientName,
      projectName: task.projectName,
      priority: task.priority,
      status: task.status,
      deadline,
      reason,
      isOverdue: reason.toLowerCase().includes('overdue'),
      isDueToday: reason.toLowerCase().includes('today'),
      actionLabel: task.status === 'Completed' ? 'View' : task.status === 'InProgress' ? 'Complete' : 'Start'
    };
  }

  private emptyRecommendation(): TaskRecommendation {
    return {
      taskId: '',
      title: '',
      clientName: '',
      projectName: '',
      priority: 'Low',
      status: 'Todo',
      deadline: new Date().toISOString(),
      reason: '',
      isOverdue: false,
      isDueToday: false,
      actionLabel: 'Start'
    };
  }

  private defaultReason(priority: TaskPriority, deadline?: string | null): string {
    if (!deadline) {
      return `${priority} priority`;
    }
    return 'Due today';
  }
}
