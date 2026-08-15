import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { SprintService } from '../../core/services/sprint.service';
import { SprintDetails, SprintSummary, SprintUpsertRequest } from '../../core/models/sprint.models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-sprints',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './sprints.component.scss',
  templateUrl: './sprints.component.html'
})
export class SprintsComponent implements OnInit {
  private readonly sprintService = inject(SprintService);
  private readonly fb = inject(FormBuilder);

  readonly sprints = signal<SprintSummary[]>([]);
  readonly selectedSprint = signal<SprintDetails | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly formError = signal('');
  readonly modalOpen = signal(false);
  readonly selectedSprintId = signal('');
  readonly submitting = signal(false);

  readonly form = this.fb.group({
    name: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    status: ['Planned', Validators.required]
  });

  ngOnInit(): void {
    this.loadSprints();
  }

  loadSprints(): void {
    this.loading.set(true);
    this.error.set('');

    this.sprintService
      .getSprints()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load sprint data right now.');
          return of([] as SprintSummary[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((sprints) => {
        this.sprints.set(sprints);
      });
  }

  completionPercent(): number {
    const sprint = this.selectedSprint();
    if (!sprint) {
      return 0;
    }

    const done = sprint.completedTaskCount;
    const total = Math.max(sprint.taskCount, 1);
    return Math.round((done / total) * 100);
  }

  openCreateSprint(): void {
    this.selectedSprintId.set('');
    this.form.reset({ status: 'Planned' });
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  selectSprint(sprint: SprintSummary): void {
    this.selectedSprintId.set(sprint.id);
    this.sprintService.getSprint(sprint.id).subscribe((details) => {
      this.selectedSprint.set(details);
      this.form.patchValue({
        name: details.name,
        startDate: this.toDateInput(details.startDate),
        endDate: this.toDateInput(details.endDate),
        status: details.status
      });
      this.formError.set('');
      this.modalOpen.set(true);
      lockBodyScroll();
    });
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.selectedSprintId.set('');
    this.selectedSprint.set(null);
    this.form.reset({ status: 'Planned' });
    this.formError.set('');
    unlockBodyScroll();
  }

  saveSprint(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required sprint fields.');
      return;
    }

    const request = this.form.getRawValue() as SprintUpsertRequest;
    this.submitting.set(true);
    this.formError.set('');
    const action$ = this.selectedSprintId()
      ? this.sprintService.updateSprint(this.selectedSprintId(), request)
      : this.sprintService.createSprint(request);

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe(() => {
        this.closeModal();
        this.loadSprints();
      }, () => {
        this.formError.set('We could not save this sprint right now. Please try again.');
      });
  }

  removeSprint(id: string): void {
    this.sprintService.deleteSprint(id).subscribe(() => this.loadSprints());
  }

  private toDateInput(value: string): string {
    return new Date(value).toISOString().slice(0, 10);
  }
}
