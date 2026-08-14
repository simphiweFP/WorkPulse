import { Component, input, output } from '@angular/core';
import { PriorityBadgeComponent } from '../priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { TaskRecommendation } from '../../models/task.models';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [PriorityBadgeComponent, StatusBadgeComponent],
  template: `
    <article class="task-card">
      <div class="task-card__header">
        <div>
          <h3>{{ task().title }}</h3>
          <p>{{ task().clientName }} / {{ task().projectName }}</p>
        </div>
        <app-priority-badge [priority]="task().priority" />
      </div>

      <div class="task-card__meta">
        <app-status-badge [status]="task().status" />
        <span>{{ deadlineLabel() }}</span>
      </div>

      <p class="reason"><strong>Why this matters:</strong> {{ task().reason }}</p>

      <div class="task-card__actions">
        @if (actionLabel()) {
          <button type="button" (click)="action.emit(task())">{{ actionLabel() }}</button>
        }
        <button type="button" class="secondary" (click)="view.emit(task())">View</button>
      </div>
    </article>
  `
})
export class TaskCardComponent {
  task = input.required<TaskRecommendation>();
  actionLabel = input<TaskRecommendation['actionLabel'] | ''>('');
  action = output<TaskRecommendation>();
  view = output<TaskRecommendation>();

  deadlineLabel(): string {
    const task = this.task();
    if (task.isOverdue) return 'Overdue';
    if (task.isDueToday) return 'Due today';
    return `Due ${new Date(task.deadline).toLocaleDateString(undefined, { day: 'numeric', month: 'long' })}`;
  }
}