export type SprintStatus = 'Planned' | 'Active' | 'Completed';

export interface SprintSummary {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  taskCount: number;
  completedTaskCount: number;
}

export interface SprintDetails extends SprintSummary {
  createdAt: string;
  updatedAt: string;
}

export interface SprintUpsertRequest {
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
}
