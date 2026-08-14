import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ClientDetails } from '../../core/models/client.models';
import { ClientService } from '../../core/services/client.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      @if (loading()) {
        <app-loading-state message="Loading client details..." />
      } @else if (client(); as model) {
        <app-page-header eyebrow="Clients" [title]="model.name" subtitle="Client information, projects, and task summary." />
        <div class="panel">
          <p><strong>Contact:</strong> {{ model.contactName }} | {{ model.contactEmail }}</p>
          <p><strong>Phone:</strong> {{ model.phoneNumber }}</p>
          <p>{{ model.description }}</p>
          <p><strong>Projects:</strong> {{ model.projects }}</p>
          <p><strong>Open Tasks:</strong> {{ model.openTasks }}</p>
        </div>
      } @else {
        <app-empty-state title="Client not found" message="The client record could not be loaded." />
      }
    </section>
  `
})
export class ClientDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clientService = inject(ClientService);

  readonly client = signal<ClientDetails | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.clientService
      .getClient(id)
      .pipe(
        catchError(() => of(null)),
      )
      .subscribe((client) => {
        this.client.set(client);
        this.loading.set(false);
      });
  }
}