export type ProjectStatus = 'Active' | 'Completed' | 'Archived';

export interface ProjectSummary {
  id: string;
  name: string;
  clientName: string;
  status: ProjectStatus;
  openTasks: number;
  completedTasks: number;
}

export interface ProjectDetails extends ProjectSummary {
  description: string;
}

export interface ProjectUpsertRequest {
  name: string;
  clientId: string;
  status: ProjectStatus;
  description: string;
}