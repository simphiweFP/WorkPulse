import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiConfig } from './api.config';

export interface UserManagementItem {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  createdAt: string;
  role: string;
  isPending?: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<UserManagementItem[]> {
    return this.http.get<UserManagementItem[]>(`${apiConfig.apiBaseUrl}/users`);
  }

  updateRole(userId: string, role: string): Observable<void> {
    return this.http.patch<void>(`${apiConfig.apiBaseUrl}/users/${userId}/role`, { role });
  }
}
