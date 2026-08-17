import { TaskPriority, TaskStatus, TaskType } from '../../shared/models/task.models';

export interface TaskAssigneeOption {
  id: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  email?: string;
  activeTaskCount?: number;
  inProgressTaskCount?: number;
  completedTaskCount?: number;
}

export interface TaskAdminSummary {
  id: string;
  clientId: string;
  projectId: string;
  title: string;
  description?: string;
  type: TaskType;
  storyPoints: number;
  sprintOrder?: number | null;
  clientName: string;
  projectName: string;
  sprintId?: string | null;
  sprintName?: string;
  assigneeId?: string | null;
  assigneeName: string;
  priority: TaskPriority;
  deadline: string;
  status: TaskStatus;
}

export interface TaskUpsertRequest {
  title: string;
  description: string;
  clientId: string;
  projectId: string;
  sprintId?: string | null;
  type: TaskType;
  priority: TaskPriority;
  status: TaskStatus;
  storyPoints: number;
  sprintOrder?: number | null;
  deadline: string;
  assigneeId: string;
}
