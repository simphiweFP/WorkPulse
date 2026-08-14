import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { ClientSummary, ClientUpsertRequest } from '../../core/models/client.models';
import { ClientService } from '../../core/services/client.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      <app-page-header eyebrow="Clients" title="Clients" subtitle="Manage customer accounts, contacts, and open work in one place." />

      @if (loading()) {
        <app-loading-state message="Loading clients..." />
      } @else if (error()) {
        <section class="error-state"><p>{{ error() }}</p></section>
      } @else {
        <form class="panel form-grid" [formGroup]="form" (ngSubmit)="saveClient()">
          <label>Name<input formControlName="name" /></label>
          <label>Contact Name<input formControlName="contactName" /></label>
          <label>Contact Email<input type="email" formControlName="contactEmail" /></label>
          <label>Phone Number<input formControlName="phoneNumber" /></label>
          <label class="full-width">Description<textarea rows="4" formControlName="description"></textarea></label>
          <div class="actions full-width">
            <button type="submit" [disabled]="submitting()">{{ selectedClientId() ? 'Update' : 'Add' }} Client</button>
            @if (selectedClientId()) {
              <button type="button" class="secondary" (click)="resetForm()">Cancel</button>
            }
          </div>
        </form>

        @if (clients().length) {
          <div class="table-card">
            <table>
              <thead>
                <tr>
                  <th>Client Name</th>
                  <th>Contact</th>
                  <th>Projects</th>
                  <th>Open Tasks</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (client of clients(); track client.id) {
                  <tr>
                    <td><a [routerLink]="['/clients', client.id]">{{ client.name }}</a></td>
                    <td><div>{{ client.contactName }}</div><small>{{ client.contactEmail }}</small></td>
                    <td>{{ client.projects }}</td>
                    <td>{{ client.openTasks }}</td>
                    <td class="row-actions">
                      <button type="button" class="secondary" (click)="selectClient(client)">Edit</button>
                      <button type="button" class="danger" (click)="removeClient(client.id)">Delete</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <app-empty-state title="No clients yet." message="Add the first client to start organizing work." />
        }
      }
    </section>
  `
})
export class ClientsComponent implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly fb = inject(FormBuilder);

  readonly clients = signal<ClientSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly selectedClientId = signal('');

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
    this.clientService
      .getClients()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load clients.');
          return of([] as ClientSummary[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((clients) => this.clients.set(clients));
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
  }

  resetForm(): void {
    this.selectedClientId.set('');
    this.form.reset();
  }

  saveClient(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue() as ClientUpsertRequest;
    this.submitting.set(true);
    const action$ = this.selectedClientId() ? this.clientService.updateClient(this.selectedClientId(), request) : this.clientService.createClient(request);

    action$.pipe(finalize(() => this.submitting.set(false))).subscribe(() => {
      this.resetForm();
      this.loadClients();
    });
  }

  removeClient(id: string): void {
    this.clientService.deleteClient(id).subscribe(() => this.loadClients());
  }
}