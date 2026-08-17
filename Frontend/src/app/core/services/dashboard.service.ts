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
  topPriority: {
    id: string;
    projectId: string;
    projectName: string;
    clientId: string;
    clientName: string;
    title: string;
    description: string;
    type: 'Bug' | 'Story' | 'Improvement';
    deadline?: string | null;
    sprintOrder?: number | null;
    status: TaskStatus;
    priority: TaskPriority;
    recommendationReason: string;
    score: number;
  };
  overdue: Array<BackendTodayTaskResponse>;
  dueToday: Array<BackendTodayTaskResponse>;
  recommendedNext: Array<BackendTodayTaskResponse>;
  completedToday: Array<BackendTodayTaskResponse>;
  sprintWorkComplete?: boolean;
  sprintName?: string;
  sprintCompletedTasks?: number;
  sprintTotalTasks?: number;
  sprintCompletedPoints?: number;
  sprintTotalPoints?: number;
}

type BackendTodayTaskResponse = BackendTodayResponse['topPriority'];

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
    return this.http.get<BackendTodayResponse>(`${apiConfig.apiBaseUrl}/today`).pipe(
      map((response) => this.toTodayDashboard(response))
    );
  }

  getAdminToday(): Observable<TodayDashboardResponse> {
    return this.http.get<BackendTodayResponse>(`${apiConfig.apiBaseUrl}/today/admin`).pipe(
      map((response) => this.toTodayDashboard(response))
    );
  }

  getAdminDashboard(): Observable<AdminDashboardResponse> {
    return this.http.get<AdminDashboardResponse>(`${apiConfig.apiBaseUrl}/dashboard/admin`);
  }

  private toTodayDashboard(response: BackendTodayResponse): TodayDashboardResponse {
    const topPriority = this.toRecommendation(response.topPriority);
    const overdue = response.overdue.map((task) => this.toRecommendation(task));
    const dueToday = response.dueToday.map((task) => this.toRecommendation(task));
    const recommendedNext = response.recommendedNext.map((task) => this.toRecommendation(task));
    const completedToday = response.completedToday.map((task) => this.toRecommendation(task));

    return {
      date: new Date().toISOString(),
      summary: {
        tasksToday: response.summary.total,
        overdue: response.summary.overdue,
        deadlineToday: response.summary.deadlineToday,
        highPriority: response.summary.highPriority
      },
      topPriority: topPriority ?? this.emptyRecommendation(),
      overdue,
      dueToday,
      recommendedNext,
      completedToday,
      sprintWorkComplete: response.sprintWorkComplete,
      sprintName: response.sprintName,
      sprintCompletedTasks: response.sprintCompletedTasks,
      sprintTotalTasks: response.sprintTotalTasks,
      sprintCompletedPoints: response.sprintCompletedPoints,
      sprintTotalPoints: response.sprintTotalPoints
    };
  }

  private toRecommendation(task: BackendTodayTaskResponse): TaskRecommendation {
    if (task.id === '00000000-0000-0000-0000-000000000000') {
      return this.emptyRecommendation();
    }

    const deadline = task.deadline ?? new Date().toISOString();
    const reason = task.recommendationReason || this.defaultReason(task.priority, task.deadline);
    const deadlineDate = new Date(deadline);
    const today = new Date();
    const isOverdue = deadlineDate.getFullYear() < today.getFullYear()
      || (deadlineDate.getFullYear() === today.getFullYear() && deadlineDate.getMonth() < today.getMonth())
      || (deadlineDate.getFullYear() === today.getFullYear() && deadlineDate.getMonth() === today.getMonth() && deadlineDate.getDate() < today.getDate());
    const isDueToday = deadlineDate.getFullYear() === today.getFullYear()
      && deadlineDate.getMonth() === today.getMonth()
      && deadlineDate.getDate() === today.getDate();
    return {
      taskId: task.id,
      title: task.title,
      clientName: task.clientName,
      projectName: task.projectName,
      type: task.type,
      storyPoints: 0,
      sprintOrder: task.sprintOrder,
      priority: task.priority,
      status: task.status,
      deadline,
      reason,
      isOverdue,
      isDueToday,
      actionLabel: task.status === 'Completed' ? 'View' : task.status === 'InProgress' ? 'Complete' : 'Start'
    };
  }

  private emptyRecommendation(): TaskRecommendation {
    return {
      taskId: '',
      title: '',
      clientName: '',
      projectName: '',
      type: 'Story',
      storyPoints: 0,
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
