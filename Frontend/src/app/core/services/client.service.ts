import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { ClientDetails, ClientResponse, ClientSummary, ClientUpsertRequest } from '../models/client.models';

@Injectable({ providedIn: 'root' })
export class ClientService {
  constructor(private readonly http: HttpClient) {}

  getClients(): Observable<ClientSummary[]> {
    return this.http.get<ClientResponse[]>(`${apiConfig.apiBaseUrl}/clients`).pipe(
      map((clients) => clients.map((client) => this.toSummary(client)))
    );
  }

  getClient(id: string): Observable<ClientDetails> {
    return this.http.get<ClientDetails>(`${apiConfig.apiBaseUrl}/clients/${id}`);
  }

  createClient(request: ClientUpsertRequest): Observable<ClientDetails> {
    return this.http.post<ClientDetails>(`${apiConfig.apiBaseUrl}/clients`, request);
  }

  updateClient(id: string, request: ClientUpsertRequest): Observable<ClientDetails> {
    return this.http.put<ClientDetails>(`${apiConfig.apiBaseUrl}/clients/${id}`, request);
  }

  deleteClient(id: string): Observable<void> {
    return this.http.delete<void>(`${apiConfig.apiBaseUrl}/clients/${id}`);
  }

  private toSummary(client: ClientResponse): ClientSummary {
    return {
      id: client.id,
      name: client.name,
      contactName: client.contactName,
      contactEmail: client.contactEmail,
      phoneNumber: client.phoneNumber,
      projects: client.projectCount,
      openTasks: client.openTaskCount
    };
  }
}
