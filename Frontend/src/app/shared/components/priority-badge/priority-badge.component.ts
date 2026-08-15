import { LowerCasePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TaskPriority } from '../../models/task.models';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  imports: [LowerCasePipe],
  templateUrl: './priority-badge.component.html'
})
export class PriorityBadgeComponent {
  priority = input.required<TaskPriority>();
}
