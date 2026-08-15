import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary, ClientUpsertRequest } from '../../core/models/client.models';
import { ClientService } from '../../core/services/client.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { lockBodyScroll, unlockBodyScroll } from '../../shared/utilities/modal-state';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  styleUrl: './clients.component.scss',
  templateUrl: './clients.component.html'
})
export class ClientsComponent implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly fb = inject(FormBuilder);

  readonly clients = signal<ClientSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly selectedClientId = signal('');
  readonly modalOpen = signal(false);
  readonly search = signal('');

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
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.clients();
    }

    return this.clients().filter((client) =>
      `${client.name} ${client.contactName} ${client.contactEmail}`.toLowerCase().includes(term)
    );
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
    this.submitting.set(true);
    this.formError.set('');
    const action$ = this.selectedClientId() ? this.clientService.updateClient(this.selectedClientId(), request) : this.clientService.createClient(request);

    action$
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe(() => {
        this.closeModal();
        this.loadClients();
      }, () => {
        this.formError.set('We could not save this client right now. Please try again.');
      });
  }

  removeClient(id: string): void {
    this.clientService.deleteClient(id).subscribe(() => this.loadClients());
  }
}
