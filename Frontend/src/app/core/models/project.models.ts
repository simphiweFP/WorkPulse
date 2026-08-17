export type ProjectStatus = 'Active' | 'Completed' | 'Archived';

export interface ProjectSummary {
  id: string;
  clientId: string;
  name: string;
  clientName: string;
  totalTasks: number;
  startDate: string;
  status: ProjectStatus;
  openTasks: number;
  completedTasks: number;
}

export interface ProjectResponse {
  id: string;
  clientId: string;
  name: string;
  clientName: string;
  totalTasks: number;
  startDate: string;
  status: ProjectStatus;
  openTaskCount: number;
  completedTaskCount: number;
}

export interface ProjectDetails extends ProjectSummary {
  description: string;
}

export interface ProjectUpsertRequest {
  name: string;
  clientId: string;
  totalTasks: number;
  startDate: string;
  status: ProjectStatus;
  description: string;
}
