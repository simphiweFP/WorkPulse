import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ProjectDetails } from '../../core/models/project.models';
import { ProjectService } from '../../core/services/project.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      @if (loading()) {
        <app-loading-state message="Loading project details..." />
      } @else if (project(); as model) {
        <app-page-header eyebrow="Projects" [title]="model.name" subtitle="Project details and associated tasks." />
        <div class="panel">
          <p><strong>Client:</strong> {{ model.clientName }}</p>
          <p><strong>Status:</strong> {{ model.status }}</p>
          <p><strong>Open Tasks:</strong> {{ model.openTasks }}</p>
          <p><strong>Completed Tasks:</strong> {{ model.completedTasks }}</p>
          <p>{{ model.description }}</p>
        </div>
      } @else {
        <app-empty-state title="Project not found" message="The project record could not be loaded." />
      }
    </section>
  `
})
export class ProjectDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly projectService = inject(ProjectService);

  readonly project = signal<ProjectDetails | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.projectService
      .getProject(id)
      .pipe(catchError(() => of(null)))
      .subscribe((project) => {
        this.project.set(project);
        this.loading.set(false);
      });
  }
}