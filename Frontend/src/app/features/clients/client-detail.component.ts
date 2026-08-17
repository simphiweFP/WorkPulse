import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ClientDetails } from '../../core/models/client.models';
import { ClientService } from '../../core/services/client.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [CommonModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './client-detail.component.html'
})
export class ClientDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clientService = inject(ClientService);

  readonly client = signal<ClientDetails | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.loadClient();
  }

  loadClient(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.error.set('The client record could not be loaded.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.clientService
      .getClient(id)
      .pipe(
        catchError(() => {
          this.error.set('We could not load this client right now. Please try again.');
          return of(null);
        })
      )
      .subscribe((client) => {
        this.client.set(client);
        this.loading.set(false);
      });
  }
}
