import { LowerCasePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TaskStatus } from '../../models/task.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [LowerCasePipe],
  templateUrl: './status-badge.component.html'
})
export class StatusBadgeComponent {
  status = input.required<TaskStatus>();
}
