import { Component, input, output } from '@angular/core';
import { PriorityBadgeComponent } from '../priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { TaskRecommendation } from '../../models/task.models';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [PriorityBadgeComponent, StatusBadgeComponent],
  templateUrl: './task-card.component.html'
})
export class TaskCardComponent {
  task = input.required<TaskRecommendation>();
  actionLabel = input<string>('');
  readonlyMode = input<boolean>(false);
  action = output<TaskRecommendation>();
  view = output<TaskRecommendation>();

  deadlineLabel(): string {
    const task = this.task();
    if (task.isOverdue) return 'Overdue';
    if (task.isDueToday) return 'Due today';
    return `Due ${new Date(task.deadline).toLocaleDateString(undefined, { day: 'numeric', month: 'long' })}`;
  }
}
