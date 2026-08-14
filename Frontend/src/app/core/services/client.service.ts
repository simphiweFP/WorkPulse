import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiConfig } from './api.config';
import { ClientDetails, ClientSummary, ClientUpsertRequest } from '../models/client.models';

@Injectable({ providedIn: 'root' })
export class ClientService {
  constructor(private readonly http: HttpClient) {}

  getClients(): Observable<ClientSummary[]> {
    return this.http.get<ClientSummary[]>(`${apiConfig.apiBaseUrl}/clients`);
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
}