export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TaskStatus = 'Todo' | 'InProgress' | 'Completed';

export interface TaskRecommendation {
  taskId: string;
  title: string;
  clientName: string;
  projectName: string;
  priority: TaskPriority;
  status: TaskStatus;
  deadline: string;
  reason: string;
  isOverdue: boolean;
  isDueToday: boolean;
  actionLabel?: 'Start' | 'Complete' | 'View';
}

export interface TodayDashboardSummary {
  tasksToday: number;
  overdue: number;
  dueToday: number;
  highOrCritical: number;
}

export interface TodayDashboardResponse {
  firstName: string;
  date: string;
  summary: TodayDashboardSummary;
  overdue: TaskRecommendation[];
  dueToday: TaskRecommendation[];
  recommendedNext: TaskRecommendation[];
  completedToday: TaskRecommendation[];
}

export interface TaskActionResponse {
  taskId: string;
  status: TaskStatus;
}