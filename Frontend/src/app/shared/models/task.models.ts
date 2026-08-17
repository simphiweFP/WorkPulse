export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TaskStatus = 'Todo' | 'InProgress' | 'Completed';
export type TaskType = 'Bug' | 'Story' | 'Improvement';

export interface TaskRecommendation {
  taskId: string;
  title: string;
  clientName: string;
  projectName: string;
  type: TaskType;
  storyPoints: number;
  sprintId?: string | null;
  sprintName?: string;
  sprintOrder?: number | null;
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
  deadlineToday: number;
  highPriority: number;
}

export interface TodayDashboardResponse {
  date?: string;
  firstName?: string;
  summary: TodayDashboardSummary;
  topPriority: TaskRecommendation;
  overdue: TaskRecommendation[];
  dueToday: TaskRecommendation[];
  recommendedNext: TaskRecommendation[];
  completedToday: TaskRecommendation[];
  sprintWorkComplete?: boolean;
  sprintName?: string;
  sprintCompletedTasks?: number;
  sprintTotalTasks?: number;
  sprintCompletedPoints?: number;
  sprintTotalPoints?: number;
}

export interface TaskActionResponse {
  taskId: string;
  status: TaskStatus;
}
