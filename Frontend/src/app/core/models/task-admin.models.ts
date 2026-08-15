import { TaskPriority, TaskStatus } from '../../shared/models/task.models';

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
  clientName: string;
  projectName: string;
  sprintId?: string | null;
  sprintName?: string;
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
  priority: TaskPriority;
  deadline: string;
  assigneeId: string;
}
