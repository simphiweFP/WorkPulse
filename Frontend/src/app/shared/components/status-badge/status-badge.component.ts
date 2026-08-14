import { LowerCasePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TaskStatus } from '../../models/task.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [LowerCasePipe],
  template: `<span class="status status-{{ status() | lowercase }}">{{ status() }}</span>`
})
export class StatusBadgeComponent {
  status = input.required<TaskStatus>();
}