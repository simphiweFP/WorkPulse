import { LowerCasePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TaskPriority } from '../../models/task.models';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  imports: [LowerCasePipe],
  template: `<span class="priority priority-{{ priority() | lowercase }}">{{ priority() }}</span>`
})
export class PriorityBadgeComponent {
  priority = input.required<TaskPriority>();
}