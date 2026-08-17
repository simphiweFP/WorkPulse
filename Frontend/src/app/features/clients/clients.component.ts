import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary, ClientUpsertRequest } from '../../core/models/client.models';
import { ClientService } from '../../core/services/client.service';
import { mapApiError } from '../../core/errors/api-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './clients.component.scss',
  templateUrl: './clients.component.html'
})
export class ClientsComponent implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly alerts = inject(FeedbackAlertService);

  readonly clients = signal<ClientSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly selectedClientId = signal('');
  readonly modalOpen = signal(false);
  // Search removed per request

  readonly form = this.fb.group({
    name: ['', Validators.required],
    contactName: ['', Validators.required],
    contactEmail: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    description: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.loading.set(true);
    this.error.set('');
    this.clientService
      .getClients()
      .pipe(
        catchError(() => {
          this.error.set('We could not load clients right now. Please try again.');
          return of([] as ClientSummary[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((clients) => this.clients.set(clients));
  }

  filteredClients(): ClientSummary[] {
    // Search removed — show all clients
    return this.clients();
  }

  openCreateClient(): void {
    this.selectedClientId.set('');
    this.form.reset();
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  selectClient(client: ClientSummary): void {
    this.selectedClientId.set(client.id);
    this.form.patchValue({
      name: client.name,
      contactName: client.contactName,
      contactEmail: client.contactEmail,
      phoneNumber: client.phoneNumber,
      description: ''
    });
    this.formError.set('');
    this.modalOpen.set(true);
    lockBodyScroll();
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.selectedClientId.set('');
    this.form.reset();
    this.formError.set('');
    unlockBodyScroll();
  }

  saveClient(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required client fields.');
      return;
    }

    const request = this.form.getRawValue() as ClientUpsertRequest;
    const email = request.contactEmail.trim().toLowerCase();
    const duplicateExists = this.clients().some((client) => {
      const sameEmail = client.contactEmail.trim().toLowerCase() === email;
      const sameClient = client.id === this.selectedClientId();
      return sameEmail && !sameClient;
    });

    if (duplicateExists) {
      this.formError.set('A client with this email already exists.');
      void this.alerts.error('Conflict', 'A client with this email already exists.');
      return;
    }

    this.submitting.set(true);
    this.formError.set('');
    const action$ = this.selectedClientId() ? this.clientService.updateClient(this.selectedClientId(), request) : this.clientService.createClient(request);

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          void this.alerts.success(
            this.selectedClientId() ? 'Client updated' : 'Client created',
            this.selectedClientId() ? 'The client was updated successfully.' : 'The client was created successfully.'
          );
          this.closeModal();
          this.loadClients();
        },
        error: (error) => {
          const appError = mapApiError(error);
          this.formError.set(appError.message);
          void this.alerts.error(appError.title, appError.message);
        }
      });
  }

  async removeClient(id: string): Promise<void> {
    if (!(await this.alerts.confirmDestructive('Delete client?', 'This client and its related data will be permanently removed.', 'Delete'))) {
      return;
    }

    this.clientService.deleteClient(id).subscribe({
      next: () => {
        void this.alerts.success('Client deleted', 'The client was deleted successfully.');
        this.loadClients();
      },
      error: () => {
        void this.alerts.error('Delete failed', 'We could not delete this client right now. Please try again.');
      }
    });
  }

  openProjects(): void {
    void this.router.navigate(['/projects']);
  }
}
