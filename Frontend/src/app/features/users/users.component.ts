import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { TaskService } from '../../core/services/task.service';

interface DeveloperRow {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  activeTaskCount: number;
  inProgressTaskCount: number;
  completedTaskCount: number;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './users.component.scss',
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  readonly developers = signal<DeveloperRow[]>([]);

  readonly memberCount = computed(() => this.developers().length);

  readonly totalActiveTasks = computed(() => this.developers().reduce((sum, developer) => sum + developer.activeTaskCount, 0));
  readonly totalInProgressTasks = computed(() => this.developers().reduce((sum, developer) => sum + developer.inProgressTaskCount, 0));
  readonly totalCompletedTasks = computed(() => this.developers().reduce((sum, developer) => sum + developer.completedTaskCount, 0));

  ngOnInit(): void {
    this.taskService.getDevelopers().subscribe((developers) => this.developers.set(developers.map((developer) => ({
      id: developer.id,
      firstName: developer.firstName,
      lastName: developer.lastName,
      fullName: `${developer.firstName} ${developer.lastName}`,
      email: developer.email ?? '',
      activeTaskCount: developer.activeTaskCount ?? 0,
      inProgressTaskCount: developer.inProgressTaskCount ?? 0,
      completedTaskCount: developer.completedTaskCount ?? 0
    }))));
  }
}
