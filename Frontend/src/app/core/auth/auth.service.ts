import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { apiConfig } from '../services/api.config';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenStorageKey = 'workpulse_token';
  private readonly userStorageKey = 'workpulse_user';
  private readonly currentUserSubject = new BehaviorSubject<User | null>(this.getStoredUser());

  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiConfig.authBaseUrl}/register`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiConfig.authBaseUrl}/login`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.userStorageKey);
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${apiConfig.authBaseUrl}/me`).pipe(
      tap((user) => {
        this.currentUserSubject.next(user);
        localStorage.setItem(this.userStorageKey, JSON.stringify(user));
      })
    );
  }

  isAuthenticated(): Observable<boolean> {
    return this.currentUser$.pipe(map((user) => user !== null && !!this.getToken()));
  }

  isAuthenticatedSnapshot(): boolean {
    return this.currentUserSubject.value !== null && !!this.getToken();
  }

  hasRole(role: string): Observable<boolean> {
    return this.currentUser$.pipe(map((user) => user?.role === role));
  }

  getCurrentUserSnapshot(): User | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenStorageKey, response.token);
    localStorage.setItem(this.userStorageKey, JSON.stringify(response.user));
    this.currentUserSubject.next(response.user);
  }

  private getStoredUser(): User | null {
    const raw = localStorage.getItem(this.userStorageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }
}
