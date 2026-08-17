export type SprintStatus = 'Planned' | 'Active' | 'Completed';

export interface SprintSummary {
  id: string;
  projectId: string;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  totalTasks: number;
  taskCount: number;
  completedTaskCount: number;
  totalPoints: number;
  completedPoints: number;
}

export interface SprintDetails extends SprintSummary {
  createdAt: string;
  updatedAt: string;
}

export interface SprintUpsertRequest {
  projectId: string;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  totalTasks: number;
}
