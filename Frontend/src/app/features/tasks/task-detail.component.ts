import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { TaskService } from '../../core/services/task.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TaskAdminSummary } from '../../core/models/task-admin.models';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, DatePipe, PageHeaderComponent],
  styleUrl: './task-detail.component.scss',
  templateUrl: './task-detail.component.html'
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly taskService = inject(TaskService);

  readonly task = signal<TaskAdminSummary | null>(null);
  readonly loadError = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.taskService
      .getTask(id)
      .pipe(
        catchError(() => {
          this.loadError.set('We could not load this task right now. Please try again.');
          return of(null);
        })
      )
      .subscribe((task) => {
        if (!task) {
          return;
        }

        this.loadError.set('');

        this.task.set({
          id: task.id,
          clientId: task.clientId,
          projectId: task.projectId,
          title: task.title,
          clientName: task.clientName,
          projectName: task.projectName,
          sprintName: task.sprintName,
          assigneeName: task.assignedUserName,
          priority: task.priority,
          deadline: task.deadline ?? task.createdAt,
          status: task.status
        });
      });
  }
}
