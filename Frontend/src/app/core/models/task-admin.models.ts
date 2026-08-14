import { TaskPriority, TaskStatus } from '../../shared/models/task.models';

export interface TaskAssigneeOption {
  id: string;
  firstName: string;
  lastName: string;
}

export interface TaskAdminSummary {
  id: string;
  title: string;
  clientName: string;
  projectName: string;
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
  priority: TaskPriority;
  deadline: string;
  assigneeId: string;
}